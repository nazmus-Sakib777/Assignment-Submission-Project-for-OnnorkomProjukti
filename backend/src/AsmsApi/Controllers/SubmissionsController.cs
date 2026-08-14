using AsmsApi.Data;
using AsmsApi.DTOs;
using AsmsApi.Models;
using AsmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsmsApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubmissionsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Student submits an answer to a published assignment before its deadline.</summary>
    [HttpPost("assignments/{assignmentId:int}/submissions")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(SubmissionDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<SubmissionDto>> Submit(int assignmentId, CreateSubmissionRequest request)
    {
        var assignment = await _db.Assignments.Include(a => a.Subject).FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment is null) return NotFound();

        if (!BusinessRules.StudentCanAccessAssignment(_currentUser.ClassRoomId, assignment.Subject!.ClassRoomId))
            return Forbid();

        var existing = await _db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == _currentUser.UserId);
        if (existing is not null)
            return BadRequest(new { error = "You have already submitted this assignment. Use update instead." });

        if (!BusinessRules.CanSubmit(assignment, DateTime.UtcNow))
            return BadRequest(new { error = "This assignment is not open for submission (draft, closed, or past deadline)." });

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = _currentUser.UserId,
            AnswerText = request.AnswerText,
            AttachmentUrl = request.AttachmentUrl,
            SubmittedAt = DateTime.UtcNow,
            Status = BusinessRules.ResolveSubmissionStatus(assignment, DateTime.UtcNow, isResubmission: false)
        };

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = submission.Id }, await MapToDtoAsync(submission));
    }

    /// <summary>Student updates their own submission before the deadline, if resubmission is allowed.</summary>
    [HttpPut("submissions/{id:int}")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(SubmissionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SubmissionDto>> UpdateSubmission(int id, UpdateSubmissionRequest request)
    {
        var submission = await _db.Submissions.Include(s => s.Assignment).FirstOrDefaultAsync(s => s.Id == id);
        if (submission is null) return NotFound();

        if (submission.StudentId != _currentUser.UserId) return Forbid();

        if (!BusinessRules.CanUpdateSubmission(submission.Assignment!, DateTime.UtcNow))
            return BadRequest(new { error = "This submission can no longer be updated (deadline passed, resubmission disabled, or assignment closed)." });

        submission.AnswerText = request.AnswerText;
        submission.AttachmentUrl = request.AttachmentUrl;
        submission.UpdatedAt = DateTime.UtcNow;
        submission.Status = BusinessRules.ResolveSubmissionStatus(submission.Assignment!, DateTime.UtcNow, isResubmission: true);
        // Editing a submission invalidates any previous grade.
        submission.Marks = null;
        submission.TeacherFeedback = null;
        submission.GradedAt = null;
        submission.GradedByTeacherId = null;

        await _db.SaveChangesAsync();
        return Ok(await MapToDtoAsync(submission));
    }

    /// <summary>Get a single submission. Owning student, the assignment's teacher, or Admin.</summary>
    [HttpGet("submissions/{id:int}")]
    [ProducesResponseType(typeof(SubmissionDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SubmissionDto>> GetById(int id)
    {
        var submission = await _db.Submissions.Include(s => s.Assignment).FirstOrDefaultAsync(s => s.Id == id);
        if (submission is null) return NotFound();

        var canView = _currentUser.Role switch
        {
            UserRole.Admin => true,
            UserRole.Teacher => submission.Assignment!.CreatedByTeacherId == _currentUser.UserId,
            UserRole.Student => submission.StudentId == _currentUser.UserId,
            _ => false
        };
        if (!canView) return Forbid();

        return Ok(await MapToDtoAsync(submission));
    }

    /// <summary>List all submissions for an assignment. Owning teacher or Admin.</summary>
    [HttpGet("assignments/{assignmentId:int}/submissions")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(IEnumerable<SubmissionDto>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetForAssignment(int assignmentId)
    {
        var assignment = await _db.Assignments.FindAsync(assignmentId);
        if (assignment is null) return NotFound();
        if (!BusinessRules.CanGrade(_currentUser.Role, _currentUser.UserId, assignment.CreatedByTeacherId))
            return Forbid();

        var submissions = await _db.Submissions.Include(s => s.Assignment).Include(s => s.Student)
            .Where(s => s.AssignmentId == assignmentId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return Ok(submissions.Select(MapToDto));
    }

    /// <summary>Grade a submission with marks and feedback. Owning teacher or Admin.</summary>
    [HttpPost("submissions/{id:int}/grade")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(SubmissionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SubmissionDto>> Grade(int id, GradeSubmissionRequest request)
    {
        var submission = await _db.Submissions.Include(s => s.Assignment).FirstOrDefaultAsync(s => s.Id == id);
        if (submission is null) return NotFound();

        if (!BusinessRules.CanGrade(_currentUser.Role, _currentUser.UserId, submission.Assignment!.CreatedByTeacherId))
            return Forbid();

        try
        {
            BusinessRules.ValidateMarks(request.Marks, submission.Assignment!.MaxMarks);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        submission.Marks = request.Marks;
        submission.TeacherFeedback = request.TeacherFeedback;
        submission.Status = SubmissionStatus.Graded;
        submission.GradedAt = DateTime.UtcNow;
        submission.GradedByTeacherId = _currentUser.UserId;

        await _db.SaveChangesAsync();
        return Ok(await MapToDtoAsync(submission));
    }

    /// <summary>Manually change a submission's status (e.g. Returned for revision). Owning teacher or Admin.</summary>
    [HttpPatch("submissions/{id:int}/status")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(SubmissionDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SubmissionDto>> ChangeStatus(int id, ChangeSubmissionStatusRequest request)
    {
        var submission = await _db.Submissions.Include(s => s.Assignment).FirstOrDefaultAsync(s => s.Id == id);
        if (submission is null) return NotFound();

        if (!BusinessRules.CanGrade(_currentUser.Role, _currentUser.UserId, submission.Assignment!.CreatedByTeacherId))
            return Forbid();

        submission.Status = request.Status;
        await _db.SaveChangesAsync();
        return Ok(await MapToDtoAsync(submission));
    }

    // ---- helpers ----

    private async Task<SubmissionDto> MapToDtoAsync(Submission s)
    {
        var student = await _db.Users.FindAsync(s.StudentId);
        var assignment = s.Assignment ?? await _db.Assignments.FindAsync(s.AssignmentId);
        return new SubmissionDto(s.Id, s.AssignmentId, assignment?.Title ?? "", s.StudentId, student?.FullName ?? "",
            s.AnswerText, s.AttachmentUrl, s.Status, s.Marks, assignment?.MaxMarks, s.TeacherFeedback,
            s.SubmittedAt, s.UpdatedAt, s.GradedAt, s.Status == SubmissionStatus.Late);
    }

    private static SubmissionDto MapToDto(Submission s) =>
        new(s.Id, s.AssignmentId, s.Assignment?.Title ?? "", s.StudentId, s.Student?.FullName ?? "",
            s.AnswerText, s.AttachmentUrl, s.Status, s.Marks, s.Assignment?.MaxMarks, s.TeacherFeedback,
            s.SubmittedAt, s.UpdatedAt, s.GradedAt, s.Status == SubmissionStatus.Late);
}
