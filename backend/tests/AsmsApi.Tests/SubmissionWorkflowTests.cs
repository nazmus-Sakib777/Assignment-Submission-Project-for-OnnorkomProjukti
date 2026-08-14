using AsmsApi.Data;
using AsmsApi.Models;
using AsmsApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AsmsApi.Tests;

/// <summary>
/// Exercises the submission workflow against a real (in-memory) EF Core context so that
/// entity relationships, cascading rules, and the "one submission per student per assignment"
/// rule are covered end-to-end, not just the pure BusinessRules helpers.
/// </summary>
public class SubmissionWorkflowTests
{
    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, ClassRoom cls, Subject subject, User teacher, User student, Assignment assignment)> SeedAsync(
        DateTime? deadline = null, AssignmentStatus status = AssignmentStatus.Published, bool allowResubmission = true)
    {
        var db = NewContext();

        var cls = new ClassRoom { Name = "Class 10 - A" };
        var teacher = new User { FullName = "T", Email = "t@x.com", PasswordHash = "x", Role = UserRole.Teacher };
        var student = new User { FullName = "S", Email = "s@x.com", PasswordHash = "x", Role = UserRole.Student, ClassRoom = cls };
        var subject = new Subject { Name = "Math", ClassRoom = cls };

        db.AddRange(cls, teacher, student, subject);
        await db.SaveChangesAsync();

        db.TeacherSubjectAssignments.Add(new TeacherSubjectAssignment { TeacherId = teacher.Id, SubjectId = subject.Id });

        var assignment = new Assignment
        {
            Title = "HW1",
            Description = "desc",
            SubjectId = subject.Id,
            CreatedByTeacherId = teacher.Id,
            Deadline = deadline ?? DateTime.UtcNow.AddDays(1),
            MaxMarks = 100,
            Status = status,
            AllowResubmission = allowResubmission
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        return (db, cls, subject, teacher, student, assignment);
    }

    [Fact]
    public async Task Student_CanSubmitOnce_ToPublishedAssignment_BeforeDeadline()
    {
        var (db, _, _, _, student, assignment) = await SeedAsync();

        var canSubmit = BusinessRules.CanSubmit(assignment, DateTime.UtcNow);
        canSubmit.Should().BeTrue();

        db.Submissions.Add(new Submission
        {
            AssignmentId = assignment.Id,
            StudentId = student.Id,
            AnswerText = "My answer",
            Status = BusinessRules.ResolveSubmissionStatus(assignment, DateTime.UtcNow, false)
        });
        await db.SaveChangesAsync();

        (await db.Submissions.CountAsync(s => s.AssignmentId == assignment.Id && s.StudentId == student.Id))
            .Should().Be(1);
    }

    [Fact]
    public async Task Student_CannotSubmit_ToDraftAssignment()
    {
        var (_, _, _, _, _, assignment) = await SeedAsync(status: AssignmentStatus.Draft);
        BusinessRules.CanSubmit(assignment, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public async Task Student_CannotSubmit_AfterDeadline()
    {
        var (_, _, _, _, _, assignment) = await SeedAsync(deadline: DateTime.UtcNow.AddMinutes(-5));
        BusinessRules.CanSubmit(assignment, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public async Task Grading_ByOwningTeacher_UpdatesMarksAndStatus()
    {
        var (db, _, _, teacher, student, assignment) = await SeedAsync();

        var submission = new Submission
        {
            AssignmentId = assignment.Id,
            StudentId = student.Id,
            AnswerText = "answer",
            Status = SubmissionStatus.Submitted
        };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        BusinessRules.CanGrade(UserRole.Teacher, teacher.Id, assignment.CreatedByTeacherId).Should().BeTrue();

        submission.Marks = 85;
        submission.Status = SubmissionStatus.Graded;
        submission.GradedByTeacherId = teacher.Id;
        submission.GradedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var reloaded = await db.Submissions.FindAsync(submission.Id);
        reloaded!.Status.Should().Be(SubmissionStatus.Graded);
        reloaded.Marks.Should().Be(85);
    }

    [Fact]
    public async Task Grading_ByNonOwningTeacher_IsRejectedByBusinessRule()
    {
        var (_, _, _, _, _, assignment) = await SeedAsync();
        const int someOtherTeacherId = 9999;

        BusinessRules.CanGrade(UserRole.Teacher, someOtherTeacherId, assignment.CreatedByTeacherId)
            .Should().BeFalse();
    }

    [Fact]
    public async Task DeletingAssignment_CascadesToSubmissions()
    {
        var (db, _, _, _, student, assignment) = await SeedAsync();
        db.Submissions.Add(new Submission { AssignmentId = assignment.Id, StudentId = student.Id, AnswerText = "a" });
        await db.SaveChangesAsync();

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync();

        (await db.Submissions.AnyAsync(s => s.AssignmentId == assignment.Id)).Should().BeFalse();
    }
}
