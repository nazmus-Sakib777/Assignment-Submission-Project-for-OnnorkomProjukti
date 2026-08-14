using AsmsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AsmsApi.Data;

public static class DbSeeder
{
    // NOTE: demo passwords below are intentionally simple for evaluation purposes only.
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync()) return; // already seeded

        var admin = new User
        {
            FullName = "System Admin",
            Email = "admin@asms.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin
        };

        var classRoom = new ClassRoom { Name = "Class 10 - Section A" };

        var teacher = new User
        {
            FullName = "Rahim Uddin",
            Email = "teacher@asms.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Teacher@123"),
            Role = UserRole.Teacher
        };

        var student = new User
        {
            FullName = "Karim Hasan",
            Email = "student@asms.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
            Role = UserRole.Student,
            ClassRoom = classRoom
        };

        var mathSubject = new Subject { Name = "Mathematics", ClassRoom = classRoom };
        var scienceSubject = new Subject { Name = "Science", ClassRoom = classRoom };

        db.Users.AddRange(admin, teacher, student);
        db.ClassRooms.Add(classRoom);
        db.Subjects.AddRange(mathSubject, scienceSubject);
        await db.SaveChangesAsync();

        db.TeacherSubjectAssignments.Add(new TeacherSubjectAssignment
        {
            TeacherId = teacher.Id,
            SubjectId = mathSubject.Id
        });
        await db.SaveChangesAsync();

        var assignment = new Assignment
        {
            Title = "Algebra Basics - Problem Set 1",
            Description = "Solve the 10 algebra problems attached in the class handout and explain your reasoning for each.",
            SubjectId = mathSubject.Id,
            CreatedByTeacherId = teacher.Id,
            Deadline = DateTime.UtcNow.AddDays(7),
            MaxMarks = 100,
            Status = AssignmentStatus.Published
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();
    }
}
