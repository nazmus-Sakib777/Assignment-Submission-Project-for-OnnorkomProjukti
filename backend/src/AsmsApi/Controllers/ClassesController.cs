using AsmsApi.Data;
using AsmsApi.DTOs;
using AsmsApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClassesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>List all classes/courses. Any authenticated role may view.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClassRoomDto>), 200)]
    public async Task<ActionResult<IEnumerable<ClassRoomDto>>> GetAll()
    {
        var classes = await _db.ClassRooms
            .Select(c => new ClassRoomDto(c.Id, c.Name, c.Students.Count, c.Subjects.Count))
            .ToListAsync();
        return Ok(classes);
    }

    /// <summary>Create a class/course. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassRoomDto), 201)]
    public async Task<ActionResult<ClassRoomDto>> Create(CreateClassRoomRequest request)
    {
        var classRoom = new ClassRoom { Name = request.Name.Trim() };
        _db.ClassRooms.Add(classRoom);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { }, new ClassRoomDto(classRoom.Id, classRoom.Name, 0, 0));
    }

    /// <summary>Delete a class/course. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var classRoom = await _db.ClassRooms.FindAsync(id);
        if (classRoom is null) return NotFound();
        _db.ClassRooms.Remove(classRoom);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>List subjects for a class. Any authenticated role may view.</summary>
    [HttpGet("{id:int}/subjects")]
    [ProducesResponseType(typeof(IEnumerable<SubjectDto>), 200)]
    public async Task<ActionResult<IEnumerable<SubjectDto>>> GetSubjects(int id)
    {
        var subjects = await _db.Subjects.Include(s => s.ClassRoom)
            .Where(s => s.ClassRoomId == id)
            .Select(s => new SubjectDto(s.Id, s.Name, s.ClassRoomId, s.ClassRoom!.Name))
            .ToListAsync();
        return Ok(subjects);
    }

    /// <summary>Create a subject under a class. Admin only.</summary>
    [HttpPost("subjects")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SubjectDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<SubjectDto>> CreateSubject(CreateSubjectRequest request)
    {
        var classRoom = await _db.ClassRooms.FindAsync(request.ClassRoomId);
        if (classRoom is null) return BadRequest(new { error = "ClassRoomId does not exist." });

        var subject = new Subject { Name = request.Name.Trim(), ClassRoomId = request.ClassRoomId };
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSubjects), new { id = classRoom.Id },
            new SubjectDto(subject.Id, subject.Name, classRoom.Id, classRoom.Name));
    }

    /// <summary>Assign a teacher to teach a subject. Admin only.</summary>
    [HttpPost("subjects/assign-teacher")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AssignTeacher(AssignTeacherRequest request)
    {
        var teacher = await _db.Users.FindAsync(request.TeacherId);
        if (teacher is null || teacher.Role != UserRole.Teacher)
            return BadRequest(new { error = "TeacherId does not refer to a valid teacher." });

        if (!await _db.Subjects.AnyAsync(s => s.Id == request.SubjectId))
            return BadRequest(new { error = "SubjectId does not exist." });

        var alreadyAssigned = await _db.TeacherSubjectAssignments
            .AnyAsync(t => t.TeacherId == request.TeacherId && t.SubjectId == request.SubjectId);
        if (alreadyAssigned) return NoContent();

        _db.TeacherSubjectAssignments.Add(new TeacherSubjectAssignment
        {
            TeacherId = request.TeacherId,
            SubjectId = request.SubjectId
        });
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
