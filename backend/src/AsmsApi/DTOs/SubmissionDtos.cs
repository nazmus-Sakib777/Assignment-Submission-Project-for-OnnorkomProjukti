using System.ComponentModel.DataAnnotations;
using AsmsApi.Models;

namespace AsmsApi.DTOs;

public record CreateSubmissionRequest(
    [Required] string AnswerText,
    string? AttachmentUrl
);

public record UpdateSubmissionRequest(
    [Required] string AnswerText,
    string? AttachmentUrl
);

public record GradeSubmissionRequest(
    [Required, Range(0, 1000)] decimal Marks,
    string? TeacherFeedback
);

public record ChangeSubmissionStatusRequest(
    [Required] SubmissionStatus Status
);

public record SubmissionDto(
    int Id,
    int AssignmentId,
    string AssignmentTitle,
    int StudentId,
    string StudentName,
    string AnswerText,
    string? AttachmentUrl,
    SubmissionStatus Status,
    decimal? Marks,
    int? MaxMarks,
    string? TeacherFeedback,
    DateTime SubmittedAt,
    DateTime? UpdatedAt,
    DateTime? GradedAt,
    bool IsLate
);
