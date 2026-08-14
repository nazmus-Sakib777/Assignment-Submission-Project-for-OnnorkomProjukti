using AsmsApi.Data;
using AsmsApi.DTOs;
using AsmsApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>List all users. Admin only.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll([FromQuery] UserRole? role)
    {
        var query = _db.Users.Include(u => u.ClassRoom).AsQueryable();
        if (role.HasValue) query = query.Where(u => u.Role == role.Value);

        var users = await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
        return Ok(users.Select(MapToDto));
    }

    /// <summary>Create a new user (Admin, Teacher, or Student). Admin only.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var email = request.Email.ToLower().Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return BadRequest(new { error = "A user with this email already exists." });

        if (request.Role == UserRole.Student && request.ClassRoomId is null)
            return BadRequest(new { error = "ClassRoomId is required for students." });

        if (request.ClassRoomId is not null && !await _db.ClassRooms.AnyAsync(c => c.Id == request.ClassRoomId))
            return BadRequest(new { error = "ClassRoomId does not exist." });

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            ClassRoomId = request.Role == UserRole.Student ? request.ClassRoomId : null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { }, MapToDto(user));
    }

    /// <summary>Update a user's name, active status, and class assignment. Admin only.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.Include(u => u.ClassRoom).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        user.FullName = request.FullName.Trim();
        user.IsActive = request.IsActive;
        if (user.Role == UserRole.Student) user.ClassRoomId = request.ClassRoomId;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(user));
    }

    /// <summary>Deactivate (soft-delete) a user. Admin only. Users are never hard-deleted to preserve submission history.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static UserDto MapToDto(User u) =>
        new(u.Id, u.FullName, u.Email, u.Role, u.IsActive, u.ClassRoomId, u.ClassRoom?.Name);
}
