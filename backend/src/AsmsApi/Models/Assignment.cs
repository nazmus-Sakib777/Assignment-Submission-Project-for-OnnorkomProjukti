using System.ComponentModel.DataAnnotations;

namespace AsmsApi.Models;

public class Assignment
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public int CreatedByTeacherId { get; set; }
    public User? CreatedByTeacher { get; set; }

    [Required]
    public DateTime Deadline { get; set; }

    [Range(1, 1000)]
    public int MaxMarks { get; set; } = 100;

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;

    // If true, students may update their submission until the deadline.
    public bool AllowResubmission { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
