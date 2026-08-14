using System.ComponentModel.DataAnnotations;
using AsmsApi.Models;

namespace AsmsApi.DTOs;

public record CreateAssignmentRequest(
    [Required, MaxLength(200)] string Title,
    [Required] string Description,
    [Required] int SubjectId,
    [Required] DateTime Deadline,
    [Range(1, 1000)] int MaxMarks,
    bool AllowResubmission,
    bool PublishNow
);

public record UpdateAssignmentRequest(
    [Required, MaxLength(200)] string Title,
    [Required] string Description,
    [Required] DateTime Deadline,
    [Range(1, 1000)] int MaxMarks,
    bool AllowResubmission
);

public record AssignmentDto(
    int Id,
    string Title,
    string Description,
    int SubjectId,
    string SubjectName,
    int ClassRoomId,
    string ClassRoomName,
    int CreatedByTeacherId,
    string CreatedByTeacherName,
    DateTime Deadline,
    int MaxMarks,
    AssignmentStatus Status,
    bool AllowResubmission,
    DateTime CreatedAt,
    int SubmissionCount,
    // Present only when the requester is a student: their own submission status, if any.
    SubmissionDto? MySubmission
);
