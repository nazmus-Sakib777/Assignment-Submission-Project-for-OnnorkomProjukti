using AsmsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AsmsApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubjectAssignment> TeacherSubjectAssignments => Set<TeacherSubjectAssignment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.ClassRoom)
                .WithMany(c => c.Students)
                .HasForeignKey(u => u.ClassRoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Subject>(e =>
        {
            e.HasOne(s => s.ClassRoom)
                .WithMany(c => c.Subjects)
                .HasForeignKey(s => s.ClassRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeacherSubjectAssignment>(e =>
        {
            e.HasIndex(t => new { t.TeacherId, t.SubjectId }).IsUnique();
            e.HasOne(t => t.Teacher)
                .WithMany(u => u.TeacherSubjectAssignments)
                .HasForeignKey(t => t.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Subject)
                .WithMany(s => s.TeacherAssignments)
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Assignment>(e =>
        {
            e.Property(a => a.MaxMarks).HasDefaultValue(100);
            e.HasOne(a => a.Subject)
                .WithMany(s => s.Assignments)
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.CreatedByTeacher)
                .WithMany(u => u.CreatedAssignments)
                .HasForeignKey(a => a.CreatedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Submission>(e =>
        {
            e.Property(s => s.Marks).HasColumnType("decimal(6,2)");
            e.HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();
            e.HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Student)
                .WithMany(u => u.Submissions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
