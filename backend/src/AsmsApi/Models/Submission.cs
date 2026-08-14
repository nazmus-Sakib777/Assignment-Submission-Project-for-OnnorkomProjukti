using System.ComponentModel.DataAnnotations;

namespace AsmsApi.Models;

public class Submission
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    [Required]
    public string AnswerText { get; set; } = string.Empty;

    // Optional link to an uploaded file (e.g. stored in /uploads or external storage).
    public string? AttachmentUrl { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public decimal? Marks { get; set; }

    public string? TeacherFeedback { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public int? GradedByTeacherId { get; set; }
}
