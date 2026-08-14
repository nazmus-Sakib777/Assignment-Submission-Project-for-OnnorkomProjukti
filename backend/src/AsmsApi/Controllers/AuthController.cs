using AsmsApi.Data;
using AsmsApi.DTOs;
using AsmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;

    public AuthController(AppDbContext db, IJwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    /// <summary>Authenticate with email/password and receive a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.Include(u => u.ClassRoom)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid email or password." });

        var (token, expiresAt) = _jwt.GenerateToken(user);

        return Ok(new LoginResponse(token, expiresAt, MapToDto(user)));
    }

    /// <summary>Return the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<ActionResult<UserDto>> Me([FromServices] ICurrentUserService currentUser)
    {
        var user = await _db.Users.Include(u => u.ClassRoom)
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId);
        if (user is null) return NotFound();
        return Ok(MapToDto(user));
    }

    private static UserDto MapToDto(Models.User u) =>
        new(u.Id, u.FullName, u.Email, u.Role, u.IsActive, u.ClassRoomId, u.ClassRoom?.Name);
}
