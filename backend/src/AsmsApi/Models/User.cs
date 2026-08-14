using System.ComponentModel.DataAnnotations;

namespace AsmsApi.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    // Only meaningful when Role == Student: the class the student belongs to.
    public int? ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TeacherSubjectAssignment> TeacherSubjectAssignments { get; set; } = new List<TeacherSubjectAssignment>();
    public ICollection<Assignment> CreatedAssignments { get; set; } = new List<Assignment>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
