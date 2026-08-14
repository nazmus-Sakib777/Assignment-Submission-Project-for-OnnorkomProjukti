using System.Security.Claims;
using AsmsApi.Models;

namespace AsmsApi.Services;

public interface ICurrentUserService
{
    int UserId { get; }
    UserRole Role { get; }
    int? ClassRoomId { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal User =>
        _accessor.HttpContext?.User ?? throw new InvalidOperationException("No HTTP context available");

    public int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("User id claim missing"));

    public UserRole Role => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)
        ?? throw new InvalidOperationException("Role claim missing"));

    public int? ClassRoomId
    {
        get
        {
            var raw = User.FindFirstValue("classRoomId");
            return string.IsNullOrEmpty(raw) ? null : int.Parse(raw);
        }
    }
}
