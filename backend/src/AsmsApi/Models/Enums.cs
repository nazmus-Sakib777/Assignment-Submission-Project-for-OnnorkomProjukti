namespace AsmsApi.Models;

public enum UserRole
{
    Admin = 0,
    Teacher = 1,
    Student = 2
}

public enum AssignmentStatus
{
    Draft = 0,
    Published = 1,
    Closed = 2
}

public enum SubmissionStatus
{
    Submitted = 0,
    Resubmitted = 1,
    Late = 2,
    Graded = 3,
    Returned = 4
}
