using System.ComponentModel.DataAnnotations;
using AsmsApi.Models;

namespace AsmsApi.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    int Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    int? ClassRoomId,
    string? ClassRoomName
);

public record CreateUserRequest(
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] UserRole Role,
    int? ClassRoomId
);

public record UpdateUserRequest(
    [Required, MaxLength(150)] string FullName,
    bool IsActive,
    int? ClassRoomId
);
