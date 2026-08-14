using AsmsApi.Models;

namespace AsmsApi.Services;

/// <summary>
/// Pure, side-effect-free business rules used across controllers and covered directly by unit tests.
/// Keeping these here (instead of scattering the logic inline in controllers) makes the most
/// important workflow/authorization rules easy to test in isolation.
/// </summary>
public static class BusinessRules
{
    public static bool IsPastDeadline(DateTime deadlineUtc, DateTime nowUtc) => nowUtc > deadlineUtc;

    /// <summary>A student may create a new submission only for a published assignment before its deadline.</summary>
    public static bool CanSubmit(Assignment assignment, DateTime nowUtc)
    {
        return assignment.Status == AssignmentStatus.Published && !IsPastDeadline(assignment.Deadline, nowUtc);
    }

    /// <summary>
    /// A student may update an existing submission only if the assignment allows resubmission,
    /// is still published, and the deadline has not passed.
    /// </summary>
    public static bool CanUpdateSubmission(Assignment assignment, DateTime nowUtc)
    {
        return assignment.AllowResubmission
            && assignment.Status == AssignmentStatus.Published
            && !IsPastDeadline(assignment.Deadline, nowUtc);
    }

    public static SubmissionStatus ResolveSubmissionStatus(Assignment assignment, DateTime submittedAtUtc, bool isResubmission)
    {
        if (IsPastDeadline(assignment.Deadline, submittedAtUtc)) return SubmissionStatus.Late;
        return isResubmission ? SubmissionStatus.Resubmitted : SubmissionStatus.Submitted;
    }

    /// <summary>Only the teacher assigned to a subject (or an Admin) may manage assignments for it.</summary>
    public static bool CanManageSubject(UserRole role, int currentUserId, IEnumerable<int> teacherIdsAssignedToSubject)
    {
        if (role == UserRole.Admin) return true;
        if (role != UserRole.Teacher) return false;
        return teacherIdsAssignedToSubject.Contains(currentUserId);
    }

    /// <summary>A student may only view/submit assignments belonging to their own class.</summary>
    public static bool StudentCanAccessAssignment(int? studentClassRoomId, int assignmentClassRoomId)
    {
        return studentClassRoomId.HasValue && studentClassRoomId.Value == assignmentClassRoomId;
    }

    /// <summary>Grading is only allowed by the teacher who owns the subject (or Admin), and only once submitted.</summary>
    public static bool CanGrade(UserRole role, int currentUserId, int assignmentOwnerTeacherId)
    {
        return role == UserRole.Admin || (role == UserRole.Teacher && currentUserId == assignmentOwnerTeacherId);
    }

    public static void ValidateMarks(decimal marks, int maxMarks)
    {
        if (marks < 0 || marks > maxMarks)
            throw new ArgumentOutOfRangeException(nameof(marks), $"Marks must be between 0 and {maxMarks}.");
    }
}
