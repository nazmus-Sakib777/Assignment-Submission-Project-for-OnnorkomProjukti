using System.ComponentModel.DataAnnotations;

namespace AsmsApi.Models;

// e.g. "Class 9 - Section A" or "BSc CSE - 3rd Year"
public class ClassRoom
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<User> Students { get; set; } = new List<User>();
}

// A subject/course that belongs to a class, e.g. "Mathematics" under "Class 9 - Section A"
public class Subject
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public int ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }

    public ICollection<TeacherSubjectAssignment> TeacherAssignments { get; set; } = new List<TeacherSubjectAssignment>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}

// Join entity: which teacher is assigned to teach which subject.
// A subject is scoped to one class, so this also implicitly assigns the teacher to that class.
public class TeacherSubjectAssignment
{
    public int Id { get; set; }

    public int TeacherId { get; set; }
    public User? Teacher { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
}
