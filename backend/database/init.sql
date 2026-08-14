-- ============================================================================
-- Assignment & Submission Management System - SQL Server schema + seed data
--
-- This script is an ALTERNATIVE to running EF Core migrations. Use one or the
-- other, not both, on a fresh database:
--   Option A (recommended): dotnet ef database update  (uses Migrations/)
--   Option B: sqlcmd -S localhost,1433 -U sa -P '<password>' -d asms -i database/init.sql
--
-- Create the database first if it doesn't exist:
--   sqlcmd -S localhost,1433 -U sa -P '<password>' -Q "CREATE DATABASE asms"
-- ============================================================================

IF OBJECT_ID('dbo.ClassRooms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClassRooms (
        Id      INT IDENTITY(1,1) PRIMARY KEY,
        Name    NVARCHAR(150) NOT NULL
    );
END;

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        FullName        NVARCHAR(150) NOT NULL,
        Email           NVARCHAR(200) NOT NULL,
        PasswordHash    NVARCHAR(MAX) NOT NULL,
        Role            INT NOT NULL,           -- 0=Admin, 1=Teacher, 2=Student
        IsActive        BIT NOT NULL DEFAULT (1),
        ClassRoomId     INT NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT FK_Users_ClassRooms FOREIGN KEY (ClassRoomId)
            REFERENCES dbo.ClassRooms (Id) ON DELETE SET NULL
    );
END;

IF OBJECT_ID('dbo.Subjects', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Subjects (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Name            NVARCHAR(150) NOT NULL,
        ClassRoomId     INT NOT NULL,
        CONSTRAINT FK_Subjects_ClassRooms FOREIGN KEY (ClassRoomId)
            REFERENCES dbo.ClassRooms (Id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.TeacherSubjectAssignments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeacherSubjectAssignments (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        TeacherId       INT NOT NULL,
        SubjectId       INT NOT NULL,
        CONSTRAINT UQ_TeacherSubject UNIQUE (TeacherId, SubjectId),
        CONSTRAINT FK_TSA_Teacher FOREIGN KEY (TeacherId)
            REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_TSA_Subject FOREIGN KEY (SubjectId)
            REFERENCES dbo.Subjects (Id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.Assignments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Assignments (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        Title               NVARCHAR(200) NOT NULL,
        Description         NVARCHAR(MAX) NOT NULL,
        SubjectId           INT NOT NULL,
        CreatedByTeacherId  INT NOT NULL,
        Deadline            DATETIME2 NOT NULL,
        MaxMarks            INT NOT NULL DEFAULT (100),
        Status              INT NOT NULL DEFAULT (0), -- 0=Draft, 1=Published, 2=Closed
        AllowResubmission   BIT NOT NULL DEFAULT (1),
        CreatedAt           DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2 NULL,
        CONSTRAINT FK_Assignments_Subjects FOREIGN KEY (SubjectId)
            REFERENCES dbo.Subjects (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Assignments_Teacher FOREIGN KEY (CreatedByTeacherId)
            REFERENCES dbo.Users (Id) ON DELETE NO ACTION
    );
END;

IF OBJECT_ID('dbo.Submissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Submissions (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        AssignmentId        INT NOT NULL,
        StudentId           INT NOT NULL,
        AnswerText          NVARCHAR(MAX) NOT NULL,
        AttachmentUrl       NVARCHAR(MAX) NULL,
        Status              INT NOT NULL DEFAULT (0), -- 0=Submitted,1=Resubmitted,2=Late,3=Graded,4=Returned
        Marks               DECIMAL(6,2) NULL,
        TeacherFeedback     NVARCHAR(MAX) NULL,
        SubmittedAt         DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2 NULL,
        GradedAt            DATETIME2 NULL,
        GradedByTeacherId   INT NULL,
        CONSTRAINT UQ_Submission_Assignment_Student UNIQUE (AssignmentId, StudentId),
        CONSTRAINT FK_Submissions_Assignments FOREIGN KEY (AssignmentId)
            REFERENCES dbo.Assignments (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Submissions_Student FOREIGN KEY (StudentId)
            REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Assignments_SubjectId')
    CREATE INDEX IX_Assignments_SubjectId ON dbo.Assignments (SubjectId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Submissions_AssignmentId')
    CREATE INDEX IX_Submissions_AssignmentId ON dbo.Submissions (AssignmentId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Submissions_StudentId')
    CREATE INDEX IX_Submissions_StudentId ON dbo.Submissions (StudentId);
GO

-- ---------------------------------------------------------------------------
-- Seed data - demo credentials (password shown, hash is bcrypt of that password)
--   Admin:   admin@asms.test   / Admin@123
--   Teacher: teacher@asms.test / Teacher@123
--   Student: student@asms.test / Student@123
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM dbo.ClassRooms WHERE Name = 'Class 10 - Section A')
    INSERT INTO dbo.ClassRooms (Name) VALUES ('Class 10 - Section A');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'admin@asms.test')
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, IsActive, ClassRoomId)
    VALUES ('System Admin', 'admin@asms.test', '$2b$11$fZOdK/tKpA8hO0RltHgRB.juuVxoAvIZc4j/8wJOkCbFnAEnylHPG', 0, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'teacher@asms.test')
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, IsActive, ClassRoomId)
    VALUES ('Rahim Uddin', 'teacher@asms.test', '$2b$11$mh..bybc1HX6YBxZx.AU6./OEIBopieDH6d0K6XjwYTMcOUukSRoq', 1, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'student@asms.test')
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, IsActive, ClassRoomId)
    SELECT 'Karim Hasan', 'student@asms.test', '$2b$11$kHFrSTnCl7/fpmOTzBZMkOUFiColjWu0nqGyTPD7LuSIYx4czBBxq', 2, 1,
           (SELECT Id FROM dbo.ClassRooms WHERE Name = 'Class 10 - Section A');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE Name = 'Mathematics')
    INSERT INTO dbo.Subjects (Name, ClassRoomId)
    SELECT 'Mathematics', Id FROM dbo.ClassRooms WHERE Name = 'Class 10 - Section A';

IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE Name = 'Science')
    INSERT INTO dbo.Subjects (Name, ClassRoomId)
    SELECT 'Science', Id FROM dbo.ClassRooms WHERE Name = 'Class 10 - Section A';
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.TeacherSubjectAssignments t
    JOIN dbo.Users u ON u.Id = t.TeacherId
    JOIN dbo.Subjects s ON s.Id = t.SubjectId
    WHERE u.Email = 'teacher@asms.test' AND s.Name = 'Mathematics'
)
    INSERT INTO dbo.TeacherSubjectAssignments (TeacherId, SubjectId)
    SELECT u.Id, s.Id
    FROM dbo.Users u, dbo.Subjects s
    WHERE u.Email = 'teacher@asms.test' AND s.Name = 'Mathematics';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Assignments WHERE Title = 'Algebra Basics - Problem Set 1')
    INSERT INTO dbo.Assignments (Title, Description, SubjectId, CreatedByTeacherId, Deadline, MaxMarks, Status, AllowResubmission)
    SELECT
        'Algebra Basics - Problem Set 1',
        'Solve the 10 algebra problems from the class handout and explain your reasoning for each.',
        s.Id,
        u.Id,
        DATEADD(DAY, 7, SYSUTCDATETIME()),
        100,
        1,
        1
    FROM dbo.Subjects s, dbo.Users u
    WHERE s.Name = 'Mathematics' AND u.Email = 'teacher@asms.test';
GO
