using AsmsApi.Data;
using AsmsApi.DTOs;
using AsmsApi.Models;
using AsmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AssignmentsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List assignments visible to the current user:
    /// Admin sees all; Teacher sees assignments for subjects they teach; Student sees
    /// published assignments for their own class.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AssignmentDto>), 200)]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetAll()
    {
        var query = _db.Assignments
            .Include(a => a.Subject).ThenInclude(s => s!.ClassRoom)
            .Include(a => a.CreatedByTeacher)
            .Include(a => a.Submissions)
            .AsQueryable();

        query = _currentUser.Role switch
        {
            UserRole.Admin => query,
            UserRole.Teacher => query.Where(a => a.CreatedByTeacherId == _currentUser.UserId),
            UserRole.Student => query.Where(a =>
                a.Status == AssignmentStatus.Published &&
                a.Subject!.ClassRoomId == _currentUser.ClassRoomId),
            _ => query.Where(a => false)
        };

        var assignments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        return Ok(assignments.Select(a => MapToDto(a, includeMySubmission: _currentUser.Role == UserRole.Student)));
    }

    /// <summary>Get a single assignment by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssignmentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<AssignmentDto>> GetById(int id)
    {
        var assignment = await LoadAssignment(id);
        if (assignment is null) return NotFound();

        if (!CanView(assignment)) return Forbid();

        return Ok(MapToDto(assignment, includeMySubmission: _currentUser.Role == UserRole.Student));
    }

    /// <summary>Create a new assignment for a subject the teacher is assigned to. Teacher or Admin.</summary>
    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(AssignmentDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<AssignmentDto>> Create(CreateAssignmentRequest request)
    {
        var subject = await _db.Subjects.Include(s => s.ClassRoom)
            .Include(s => s.TeacherAssignments)
            .FirstOrDefaultAsync(s => s.Id == request.SubjectId);
        if (subject is null) return BadRequest(new { error = "SubjectId does not exist." });

        var teacherIds = subject.TeacherAssignments.Select(t => t.TeacherId);
        if (!BusinessRules.CanManageSubject(_currentUser.Role, _currentUser.UserId, teacherIds))
            return Forbid();

        if (request.Deadline <= DateTime.UtcNow)
            return BadRequest(new { error = "Deadline must be in the future." });

        var assignment = new Assignment
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            SubjectId = request.SubjectId,
            CreatedByTeacherId = _currentUser.Role == UserRole.Admin
                ? subject.TeacherAssignments.First().TeacherId
                : _currentUser.UserId,
            Deadline = request.Deadline,
            MaxMarks = request.MaxMarks,
            AllowResubmission = request.AllowResubmission,
            Status = request.PublishNow ? AssignmentStatus.Published : AssignmentStatus.Draft
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        var reloaded = (await LoadAssignment(assignment.Id))!;
        return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, MapToDto(reloaded, false));
    }

    /// <summary>Update an assignment's details. Only the owning teacher or Admin.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(AssignmentDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<AssignmentDto>> Update(int id, UpdateAssignmentRequest request)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment is null) return NotFound();

        if (!BusinessRules.CanGrade(_currentUser.Role, _currentUser.UserId, assignment.CreatedByTeacherId))
            return Forbid();

        assignment.Title = request.Title.Trim();
        assignment.Description = request.Description;
        assignment.Deadline = request.Deadline;
        assignment.MaxMarks = request.MaxMarks;
        assignment.AllowResubmission = request.AllowResubmission;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var reloaded = (await LoadAssignment(id))!;
        return Ok(MapToDto(reloaded, false));
    }

    /// <summary>Publish a draft assignment, making it visible to students. Owning teacher or Admin.</summary>
    [HttpPost("{id:int}/publish")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Publish(int id)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment is null) return NotFound();
        if (!BusinessRules.CanGrade(_currentUser.Role, _currentUser.UserId, assignment.CreatedByTeacherId))
            return Forbid();

        assignment.Status = AssignmentStatus.Published;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Close an assignment to further submissions. Owning teacher or Admin.</summary>
    [HttpPost("{id:int}/close")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Close(int id)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment is null) return NotFound();
        if (!BusinessRules.CanGrade(_currentUser.Role, _currentUser.UserId, assignment.CreatedByTeacherId))
            return Forbid();

        assignment.Status = AssignmentStatus.Closed;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete an assignment (and its submissions). Owning teacher or Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment is null) return NotFound();
        if (!BusinessRules.CanGrade(_currentUser.Role, _currentUser.UserId, assignment.CreatedByTeacherId))
            return Forbid();

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- helpers ----

    private Task<Assignment?> LoadAssignment(int id) =>
        _db.Assignments
            .Include(a => a.Subject).ThenInclude(s => s!.ClassRoom)
            .Include(a => a.CreatedByTeacher)
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == id);

    private bool CanView(Assignment assignment)
    {
        return _currentUser.Role switch
        {
            UserRole.Admin => true,
            UserRole.Teacher => assignment.CreatedByTeacherId == _currentUser.UserId,
            UserRole.Student => assignment.Status == AssignmentStatus.Published &&
                BusinessRules.StudentCanAccessAssignment(_currentUser.ClassRoomId, assignment.Subject!.ClassRoomId),
            _ => false
        };
    }

    private AssignmentDto MapToDto(Assignment a, bool includeMySubmission)
    {
        SubmissionDto? mine = null;
        if (includeMySubmission)
        {
            var sub = a.Submissions.FirstOrDefault(s => s.StudentId == _currentUser.UserId);
            if (sub is not null)
            {
                mine = new SubmissionDto(sub.Id, sub.AssignmentId, a.Title, sub.StudentId, "", sub.AnswerText,
                    sub.AttachmentUrl, sub.Status, sub.Marks, a.MaxMarks, sub.TeacherFeedback, sub.SubmittedAt,
                    sub.UpdatedAt, sub.GradedAt, sub.Status == SubmissionStatus.Late);
            }
        }

        return new AssignmentDto(
            a.Id, a.Title, a.Description, a.SubjectId, a.Subject?.Name ?? "",
            a.Subject?.ClassRoomId ?? 0, a.Subject?.ClassRoom?.Name ?? "",
            a.CreatedByTeacherId, a.CreatedByTeacher?.FullName ?? "",
            a.Deadline, a.MaxMarks, a.Status, a.AllowResubmission, a.CreatedAt,
            a.Submissions.Count, mine);
    }
}
