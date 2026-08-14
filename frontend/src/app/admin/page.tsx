"use client";

import { FormEvent, useEffect, useState } from "react";
import RequireRole from "@/components/RequireRole";
import Navbar from "@/components/Navbar";
import { ClassesApi, UsersApi } from "@/lib/resources";
import { ClassRoom, Subject, User, UserRole } from "@/types";
import { extractErrorMessage } from "@/lib/api";

function AdminInner() {
  const [tab, setTab] = useState<"users" | "academic">("users");

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar title="Admin Dashboard" />
      <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6">
        <div className="mb-6 flex gap-2">
          <TabButton active={tab === "users"} onClick={() => setTab("users")}>
            Users
          </TabButton>
          <TabButton active={tab === "academic"} onClick={() => setTab("academic")}>
            Classes &amp; Subjects
          </TabButton>
        </div>
        {tab === "users" ? <UsersPanel /> : <AcademicPanel />}
      </main>
    </div>
  );
}

function TabButton({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      onClick={onClick}
      className={`rounded-md px-4 py-2 text-sm font-medium ${
        active ? "bg-indigo-600 text-white" : "bg-white text-slate-600 border border-slate-200"
      }`}
    >
      {children}
    </button>
  );
}

function UsersPanel() {
  const [users, setUsers] = useState<User[]>([]);
  const [classes, setClasses] = useState<ClassRoom[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<UserRole>("Student");
  const [classRoomId, setClassRoomId] = useState<number | "">("");
  const [submitting, setSubmitting] = useState(false);

  async function refresh() {
    setLoading(true);
    try {
      const [u, c] = await Promise.all([UsersApi.list(), ClassesApi.list()]);
      setUsers(u);
      setClasses(c);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    refresh();
  }, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await UsersApi.create({
        fullName,
        email,
        password,
        role,
        classRoomId: role === "Student" && classRoomId !== "" ? Number(classRoomId) : null,
      });
      setFullName("");
      setEmail("");
      setPassword("");
      setClassRoomId("");
      await refresh();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  async function toggleActive(u: User) {
    try {
      await UsersApi.update(u.id, {
        fullName: u.fullName,
        isActive: !u.isActive,
        classRoomId: u.classRoomId,
      });
      await refresh();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <div className="lg:col-span-2">
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-left text-xs font-medium uppercase text-slate-500">
              <tr>
                <th className="px-4 py-2">Name</th>
                <th className="px-4 py-2">Email</th>
                <th className="px-4 py-2">Role</th>
                <th className="px-4 py-2">Class</th>
                <th className="px-4 py-2">Status</th>
                <th className="px-4 py-2"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading && (
                <tr>
                  <td className="px-4 py-4 text-slate-400" colSpan={6}>
                    Loading…
                  </td>
                </tr>
              )}
              {!loading &&
                users.map((u) => (
                  <tr key={u.id}>
                    <td className="px-4 py-2 font-medium text-slate-900">{u.fullName}</td>
                    <td className="px-4 py-2 text-slate-600">{u.email}</td>
                    <td className="px-4 py-2 text-slate-600">{u.role}</td>
                    <td className="px-4 py-2 text-slate-600">{u.classRoomName ?? "—"}</td>
                    <td className="px-4 py-2">
                      <span
                        className={
                          u.isActive
                            ? "rounded-full bg-emerald-100 px-2 py-0.5 text-xs text-emerald-700"
                            : "rounded-full bg-rose-100 px-2 py-0.5 text-xs text-rose-700"
                        }
                      >
                        {u.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-right">
                      <button
                        onClick={() => toggleActive(u)}
                        className="text-xs font-medium text-indigo-600 hover:underline"
                      >
                        {u.isActive ? "Deactivate" : "Activate"}
                      </button>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h2 className="mb-3 text-sm font-semibold text-slate-900">Add user</h2>
        <form onSubmit={handleCreate} className="space-y-3">
          <input
            required
            placeholder="Full name"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
          />
          <input
            required
            type="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
          />
          <input
            required
            type="password"
            placeholder="Password (min 6 chars)"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
          />
          <select
            value={role}
            onChange={(e) => setRole(e.target.value as UserRole)}
            className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
          >
            <option value="Student">Student</option>
            <option value="Teacher">Teacher</option>
            <option value="Admin">Admin</option>
          </select>
          {role === "Student" && (
            <select
              value={classRoomId}
              onChange={(e) => setClassRoomId(e.target.value === "" ? "" : Number(e.target.value))}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              required
            >
              <option value="">Select class</option>
              {classes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          )}
          {error && <p className="text-xs text-rose-600">{error}</p>}
          <button
            type="submit"
            disabled={submitting}
            className="w-full rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-60"
          >
            {submitting ? "Creating…" : "Create user"}
          </button>
        </form>
      </div>
    </div>
  );
}

function AcademicPanel() {
  const [classes, setClasses] = useState<ClassRoom[]>([]);
  const [teachers, setTeachers] = useState<User[]>([]);
  const [subjectsByClass, setSubjectsByClass] = useState<Record<number, Subject[]>>({});
  const [error, setError] = useState<string | null>(null);
  const [newClassName, setNewClassName] = useState("");
  const [newSubjectName, setNewSubjectName] = useState("");
  const [subjectClassId, setSubjectClassId] = useState<number | "">("");
  const [assignTeacherId, setAssignTeacherId] = useState<number | "">("");
  const [assignSubjectId, setAssignSubjectId] = useState<number | "">("");

  async function refresh() {
    try {
      const [c, t] = await Promise.all([ClassesApi.list(), UsersApi.list("Teacher")]);
      setClasses(c);
      setTeachers(t);
      const entries = await Promise.all(c.map(async (cls) => [cls.id, await ClassesApi.subjects(cls.id)] as const));
      setSubjectsByClass(Object.fromEntries(entries));
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  useEffect(() => {
    refresh();
  }, []);

  async function handleCreateClass(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await ClassesApi.create(newClassName);
      setNewClassName("");
      await refresh();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  async function handleCreateSubject(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (subjectClassId === "") return;
    try {
      await ClassesApi.createSubject(newSubjectName, Number(subjectClassId));
      setNewSubjectName("");
      await refresh();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  async function handleAssignTeacher(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (assignTeacherId === "" || assignSubjectId === "") return;
    try {
      await ClassesApi.assignTeacher(Number(assignTeacherId), Number(assignSubjectId));
      await refresh();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  const allSubjects = Object.values(subjectsByClass).flat();

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <div className="lg:col-span-2 space-y-4">
        {classes.map((c) => (
          <div key={c.id} className="rounded-lg border border-slate-200 bg-white p-4">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-semibold text-slate-900">{c.name}</h3>
              <span className="text-xs text-slate-500">{c.studentCount} students</span>
            </div>
            <ul className="mt-2 space-y-1 text-sm text-slate-600">
              {(subjectsByClass[c.id] ?? []).map((s) => (
                <li key={s.id}>• {s.name}</li>
              ))}
              {(subjectsByClass[c.id] ?? []).length === 0 && (
                <li className="text-slate-400">No subjects yet.</li>
              )}
            </ul>
          </div>
        ))}
        {classes.length === 0 && (
          <p className="text-sm text-slate-400">No classes yet — create one to get started.</p>
        )}
      </div>

      <div className="space-y-4">
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold text-slate-900">New class/course</h2>
          <form onSubmit={handleCreateClass} className="space-y-2">
            <input
              required
              placeholder="e.g. Class 10 - Section B"
              value={newClassName}
              onChange={(e) => setNewClassName(e.target.value)}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            />
            <button className="w-full rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700">
              Add class
            </button>
          </form>
        </div>

        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold text-slate-900">New subject</h2>
          <form onSubmit={handleCreateSubject} className="space-y-2">
            <input
              required
              placeholder="e.g. Physics"
              value={newSubjectName}
              onChange={(e) => setNewSubjectName(e.target.value)}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            />
            <select
              required
              value={subjectClassId}
              onChange={(e) => setSubjectClassId(e.target.value === "" ? "" : Number(e.target.value))}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            >
              <option value="">Select class</option>
              {classes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            <button className="w-full rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700">
              Add subject
            </button>
          </form>
        </div>

        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold text-slate-900">Assign teacher to subject</h2>
          <form onSubmit={handleAssignTeacher} className="space-y-2">
            <select
              required
              value={assignTeacherId}
              onChange={(e) => setAssignTeacherId(e.target.value === "" ? "" : Number(e.target.value))}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            >
              <option value="">Select teacher</option>
              {teachers.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.fullName}
                </option>
              ))}
            </select>
            <select
              required
              value={assignSubjectId}
              onChange={(e) => setAssignSubjectId(e.target.value === "" ? "" : Number(e.target.value))}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            >
              <option value="">Select subject</option>
              {allSubjects.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.classRoomName})
                </option>
              ))}
            </select>
            <button className="w-full rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700">
              Assign
            </button>
          </form>
        </div>

        {error && <p className="text-xs text-rose-600">{error}</p>}
      </div>
    </div>
  );
}

export default function AdminPage() {
  return (
    <RequireRole allow={["Admin"]}>
      <AdminInner />
    </RequireRole>
  );
}
