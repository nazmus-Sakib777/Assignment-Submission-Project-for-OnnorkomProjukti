using AsmsApi.Models;
using AsmsApi.Services;
using FluentAssertions;
using Xunit;

namespace AsmsApi.Tests;

public class BusinessRulesTests
{
    private static Assignment MakeAssignment(
        AssignmentStatus status = AssignmentStatus.Published,
        bool allowResubmission = true,
        DateTime? deadline = null,
        int maxMarks = 100)
    {
        return new Assignment
        {
            Id = 1,
            Title = "Test Assignment",
            Description = "desc",
            SubjectId = 1,
            CreatedByTeacherId = 10,
            Status = status,
            AllowResubmission = allowResubmission,
            Deadline = deadline ?? DateTime.UtcNow.AddDays(1),
            MaxMarks = maxMarks
        };
    }

    // ---- Deadline / submission window ----

    [Fact]
    public void CanSubmit_WhenPublishedAndBeforeDeadline_ReturnsTrue()
    {
        var assignment = MakeAssignment(deadline: DateTime.UtcNow.AddHours(2));
        BusinessRules.CanSubmit(assignment, DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void CanSubmit_WhenPastDeadline_ReturnsFalse()
    {
        var assignment = MakeAssignment(deadline: DateTime.UtcNow.AddHours(-1));
        BusinessRules.CanSubmit(assignment, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void CanSubmit_WhenDraft_ReturnsFalse()
    {
        var assignment = MakeAssignment(status: AssignmentStatus.Draft, deadline: DateTime.UtcNow.AddHours(2));
        BusinessRules.CanSubmit(assignment, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void CanSubmit_WhenClosed_ReturnsFalse()
    {
        var assignment = MakeAssignment(status: AssignmentStatus.Closed, deadline: DateTime.UtcNow.AddHours(2));
        BusinessRules.CanSubmit(assignment, DateTime.UtcNow).Should().BeFalse();
    }

    // ---- Resubmission rules ----

    [Fact]
    public void CanUpdateSubmission_WhenAllowedAndBeforeDeadline_ReturnsTrue()
    {
        var assignment = MakeAssignment(allowResubmission: true, deadline: DateTime.UtcNow.AddHours(2));
        BusinessRules.CanUpdateSubmission(assignment, DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void CanUpdateSubmission_WhenResubmissionDisabled_ReturnsFalse()
    {
        var assignment = MakeAssignment(allowResubmission: false, deadline: DateTime.UtcNow.AddHours(2));
        BusinessRules.CanUpdateSubmission(assignment, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void CanUpdateSubmission_WhenPastDeadline_ReturnsFalse()
    {
        var assignment = MakeAssignment(allowResubmission: true, deadline: DateTime.UtcNow.AddHours(-1));
        BusinessRules.CanUpdateSubmission(assignment, DateTime.UtcNow).Should().BeFalse();
    }

    // ---- Late-status resolution ----

    [Fact]
    public void ResolveSubmissionStatus_BeforeDeadline_FirstSubmission_IsSubmitted()
    {
        var assignment = MakeAssignment(deadline: DateTime.UtcNow.AddHours(2));
        var status = BusinessRules.ResolveSubmissionStatus(assignment, DateTime.UtcNow, isResubmission: false);
        status.Should().Be(SubmissionStatus.Submitted);
    }

    [Fact]
    public void ResolveSubmissionStatus_BeforeDeadline_Resubmission_IsResubmitted()
    {
        var assignment = MakeAssignment(deadline: DateTime.UtcNow.AddHours(2));
        var status = BusinessRules.ResolveSubmissionStatus(assignment, DateTime.UtcNow, isResubmission: true);
        status.Should().Be(SubmissionStatus.Resubmitted);
    }

    [Fact]
    public void ResolveSubmissionStatus_AfterDeadline_IsLateRegardless()
    {
        var assignment = MakeAssignment(deadline: DateTime.UtcNow.AddHours(-1));
        BusinessRules.ResolveSubmissionStatus(assignment, DateTime.UtcNow, isResubmission: false)
            .Should().Be(SubmissionStatus.Late);
        BusinessRules.ResolveSubmissionStatus(assignment, DateTime.UtcNow, isResubmission: true)
            .Should().Be(SubmissionStatus.Late);
    }

    // ---- Authorization: subject/assignment management ----

    [Fact]
    public void CanManageSubject_Admin_AlwaysTrue()
    {
        BusinessRules.CanManageSubject(UserRole.Admin, 999, new[] { 1, 2 }).Should().BeTrue();
    }

    [Fact]
    public void CanManageSubject_AssignedTeacher_ReturnsTrue()
    {
        BusinessRules.CanManageSubject(UserRole.Teacher, 5, new[] { 5, 6 }).Should().BeTrue();
    }

    [Fact]
    public void CanManageSubject_UnassignedTeacher_ReturnsFalse()
    {
        BusinessRules.CanManageSubject(UserRole.Teacher, 7, new[] { 5, 6 }).Should().BeFalse();
    }

    [Fact]
    public void CanManageSubject_Student_AlwaysFalse()
    {
        BusinessRules.CanManageSubject(UserRole.Student, 5, new[] { 5, 6 }).Should().BeFalse();
    }

    // ---- Authorization: student class scoping ----

    [Fact]
    public void StudentCanAccessAssignment_SameClass_ReturnsTrue()
    {
        BusinessRules.StudentCanAccessAssignment(studentClassRoomId: 3, assignmentClassRoomId: 3).Should().BeTrue();
    }

    [Fact]
    public void StudentCanAccessAssignment_DifferentClass_ReturnsFalse()
    {
        BusinessRules.StudentCanAccessAssignment(studentClassRoomId: 3, assignmentClassRoomId: 4).Should().BeFalse();
    }

    [Fact]
    public void StudentCanAccessAssignment_NoClassAssigned_ReturnsFalse()
    {
        BusinessRules.StudentCanAccessAssignment(studentClassRoomId: null, assignmentClassRoomId: 4).Should().BeFalse();
    }

    // ---- Authorization: grading ----

    [Fact]
    public void CanGrade_OwningTeacher_ReturnsTrue()
    {
        BusinessRules.CanGrade(UserRole.Teacher, 10, assignmentOwnerTeacherId: 10).Should().BeTrue();
    }

    [Fact]
    public void CanGrade_DifferentTeacher_ReturnsFalse()
    {
        BusinessRules.CanGrade(UserRole.Teacher, 11, assignmentOwnerTeacherId: 10).Should().BeFalse();
    }

    [Fact]
    public void CanGrade_Admin_AlwaysTrue()
    {
        BusinessRules.CanGrade(UserRole.Admin, 999, assignmentOwnerTeacherId: 10).Should().BeTrue();
    }

    [Fact]
    public void CanGrade_Student_AlwaysFalse()
    {
        BusinessRules.CanGrade(UserRole.Student, 10, assignmentOwnerTeacherId: 10).Should().BeFalse();
    }

    // ---- Marks validation ----

    [Theory]
    [InlineData(-1, 100)]
    [InlineData(101, 100)]
    public void ValidateMarks_OutOfRange_Throws(decimal marks, int max)
    {
        var act = () => BusinessRules.ValidateMarks(marks, max);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 100)]
    [InlineData(55.5, 100)]
    public void ValidateMarks_InRange_DoesNotThrow(decimal marks, int max)
    {
        var act = () => BusinessRules.ValidateMarks(marks, max);
        act.Should().NotThrow();
    }
}
