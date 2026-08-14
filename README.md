# Assignment & Submission Management System

A role-based Assignment & Submission Management System for a school/college, built for the
OnnoRokom Projukti Limited Assistant Software Engineer recruitment project.

Teachers create assignments for the subjects they teach, students view and submit answers
before the deadline, and teachers review submissions and assign marks and feedback. An Admin
manages users, classes/courses, subjects, and teacher-subject assignments.

## 1. Project overview

- **Admin**: manages users (Admin/Teacher/Student), classes/courses, subjects, and assigns
  teachers to subjects.
- **Teacher**: creates/updates/deletes assignments for subjects they are assigned to, publishes
  or keeps them as drafts, views submissions, and assigns marks + feedback.
- **Student**: sees published assignments for their own class, submits an answer, can update it
  before the deadline (if resubmission is allowed), and can see their status, marks, and
  feedback.

## 2. Main features

- JWT-based login with role-based authorization enforced on every backend endpoint (not just
  hidden in the UI).
- Admin: user management (create/update/deactivate), class & subject management, teacher→subject
  assignment.
- Teacher: create/edit/publish/close/delete assignments; view and grade submissions; feedback.
- Student: browse assignments for their class; submit; edit submission before deadline (if
  allowed); view marks/feedback.
- Deadline enforcement, "late" detection, and resubmission control are enforced server-side.
- Swagger/OpenAPI docs, structured logging (Serilog), centralized error handling.
- Unit tests covering the important business rules (deadlines, resubmission, authorization,
  grading) plus EF Core workflow tests.

## 3. Technology stack

| Layer          | Technology |
|----------------|------------|
| Frontend       | Next.js 15 (App Router), React, TypeScript, Tailwind CSS, Axios |
| Backend        | ASP.NET Core 8 Web API, C#, EF Core, Swagger/OpenAPI, Serilog |
| Database       | Microsoft SQL Server (EF Core + Microsoft.EntityFrameworkCore.SqlServer) |
| Auth           | JWT Bearer tokens, BCrypt password hashing, role-based authorization |
| Testing        | xUnit, FluentAssertions, EF Core InMemory provider |

## 4. Project structure

```
asms/
├── backend/
│   ├── AsmsApi.sln
│   ├── docker-compose.yml          # local SQL Server
│   ├── database/init.sql           # raw SQL schema + seed (alternative to EF migrations)
│   ├── .env.example
│   └── src/AsmsApi/
│       ├── Controllers/            # Auth, Users, Classes, Assignments, Submissions
│       ├── Models/                 # EF Core entities + enums
│       ├── DTOs/                   # request/response records
│       ├── Data/                   # AppDbContext, DbSeeder
│       ├── Services/                # JwtService, CurrentUserService, BusinessRules
│       ├── Middleware/              # global exception handling
│       └── Program.cs
│   └── tests/AsmsApi.Tests/         # xUnit unit + workflow tests
└── frontend/
    ├── .env.example
    └── src/
        ├── app/                    # login, admin, teacher, student pages (App Router)
        ├── components/             # Navbar, Badge, RequireRole
        ├── lib/                    # api client, auth context, resource helpers
        └── types/                  # TypeScript types mirroring backend DTOs
```

## 5. Data model

- `User` (Admin/Teacher/Student, `ClassRoomId` only set for Students)
- `ClassRoom` (a class/course, e.g. "Class 10 - Section A")
- `Subject` (belongs to one `ClassRoom`)
- `TeacherSubjectAssignment` (join table: which teacher teaches which subject — this is also how
  a teacher is implicitly scoped to a class)
- `Assignment` (belongs to a `Subject`, created by a `Teacher`, has `Draft/Published/Closed`
  status)
- `Submission` (one per student per assignment — enforced by a unique constraint — with
  `Submitted/Resubmitted/Late/Graded/Returned` status, marks, and feedback)

SQL Server was chosen (over MongoDB) because the domain is inherently relational (users belong to
classes, subjects belong to classes, assignments belong to subjects, submissions reference both an
assignment and a student, with a uniqueness constraint on the pair) — this maps naturally to
foreign keys and is straightforward to enforce with a relational database.

## 6. Setup instructions

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- SQL Server 2019+ / SQL Server Express / Azure SQL (or Docker, see below)

### 6.0 Fastest path: one-command Docker stack

```bash
docker compose up --build
```

Runs SQL Server, the backend API (`http://localhost:5000`, Swagger at `/swagger`), and the
frontend (`http://localhost:3000`) together. **Caveat:** the backend image runs the compiled API
directly; it does not generate EF Core migrations for you. Follow steps 6.1–6.2 once (generate
the migration, or apply `backend/database/init.sql`) against the `sqlserver` service before
relying on the Docker backend to create the schema — otherwise it will start with an empty
database and fail on first request. `backend/Dockerfile` and `frontend/Dockerfile` are also
provided individually if you want to build/run either service on its own.

### 6.1 Database

**Option A — Docker (recommended):**

```bash
cd backend
docker compose up -d
```

This starts SQL Server (2022, Developer/Express edition image) on `localhost:1433`, with SA
password `YourStrong!Passw0rd` (matching the default connection string in `appsettings.json`).
Create the `asms` database once it's up:

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -Q "CREATE DATABASE asms"
```

**Option B — existing SQL Server install:** create a database named `asms` and update the
connection string in `appsettings.Development.json` (or via environment variables — see
`.env.example`).

The schema and demo seed data are created automatically in two possible ways:

1. **Automatic (default):** on first run, the API calls `Database.MigrateAsync()` and then
   `DbSeeder.SeedAsync()` on startup — see step 6.2. You must first generate the migration once
   (see below), since it is not pre-generated in this repository.
2. **Manual fallback:** run the raw SQL script instead:
   ```bash
   sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -d asms -i backend/database/init.sql
   ```
   Use this OR the EF migration path — not both, on the same database.

### 6.2 Backend (ASP.NET Core API)

```bash
cd backend/src/AsmsApi
dotnet restore
dotnet tool install --global dotnet-ef   # first time only
dotnet ef migrations add InitialCreate    # first time only — generates Migrations/
dotnet ef database update                 # applies the migration
dotnet run
```

The API starts on `http://localhost:5000` with Swagger UI at `http://localhost:5000/swagger`.
Copy `backend/.env.example` to configure secrets (JWT key, connection string) via environment
variables or `dotnet user-secrets` instead of committing them to `appsettings.json`.

### 6.3 Frontend (Next.js)

```bash
cd frontend
npm install
cp .env.example .env.local   # adjust NEXT_PUBLIC_API_BASE_URL if needed
npm run dev
```

The app runs on `http://localhost:3000` and expects the API at `http://localhost:5000/api` by
default.

### 6.4 Running tests

```bash
cd backend/tests/AsmsApi.Tests
dotnet test
```

Covers: deadline/submission-window rules, resubmission rules, late-status resolution,
subject/assignment/grading authorization, marks validation, and an EF Core (InMemory)
end-to-end workflow (submit → grade → cascade delete).

## 7. Demo credentials

| Role    | Email               | Password     |
|---------|---------------------|--------------|
| Admin   | admin@asms.test     | Admin@123    |
| Teacher | teacher@asms.test   | Teacher@123  |
| Student | student@asms.test   | Student@123  |

These are seeded automatically by `DbSeeder` (or by `database/init.sql` if you use the SQL
fallback). **Change or remove these before any real deployment.**

## 8. Assumptions

- A "class/course" (`ClassRoom`) and a "subject" are modeled separately: a subject always
  belongs to exactly one class, and a student belongs to exactly one class. A teacher is
  associated with a class indirectly, through the subjects they are assigned to teach.
- A teacher may teach more than one subject/class; a subject may currently only have one teacher
  assigned in the UI flow (the join table technically supports many, matching "assign a teacher
  to a subject").
- Only one submission per student per assignment is allowed; editing an existing submission
  ("update a submission before the deadline, if allowed") re-uses the same submission and clears
  any previous grade, rather than creating a new attempt/version.
- "Change the submission status when necessary" (Admin/Teacher capability) is implemented as a
  manual status-change endpoint (e.g., to mark a submission `Returned` for revision) in addition
  to the automatic status transitions (`Submitted`/`Resubmitted`/`Late`/`Graded`).
- File/attachment upload is represented as a URL field (`attachmentUrl`) rather than binary file
  upload/storage, to keep the scope focused on the core assignment/submission workflow; a real
  deployment would add object storage (e.g., S3-compatible) behind this field.
- Users are soft-deleted (`IsActive = false`) rather than hard-deleted, to preserve submission
  history and grading records.
- Admins can create assignments on behalf of a subject's assigned teacher (attributed to that
  teacher) for administrative flexibility, since the spec lists "view all assignments and
  submissions" as an Admin capability but does not explicitly forbid creation.

## 9. Known limitations

- No automated end-to-end (Playwright/Cypress) tests — coverage is at the unit/business-rule
  level on the backend only, per the "unit tests covering important business rules,
  authorization, and submission workflows" requirement.
- No password-reset/forgot-password flow.
- No file upload/storage integration (see Assumptions).
- No pagination/advanced filtering on list endpoints (listed as optional in the brief).
- EF Core migrations are not pre-generated in this repository — generate them locally with
  `dotnet ef migrations add InitialCreate` as shown in section 6.2, or use the SQL fallback in
  `database/init.sql`. The backend code itself was written and manually reviewed without a live
  `dotnet build` against the real NuGet packages (the preparation environment could reach
  `nuget.org` for the .NET SDK installer but not for package restore), so please run
  `dotnet restore && dotnet build && dotnet test` as your first step after unzipping and fix
  anything that surfaces — I'd expect it to be clean, but I want to be upfront about what was and
  wasn't verified end-to-end.
- Real-time notifications are not implemented (listed as optional in the brief).
