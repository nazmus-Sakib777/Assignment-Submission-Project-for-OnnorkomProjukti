"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import RequireRole from "@/components/RequireRole";
import Navbar from "@/components/Navbar";
import Badge from "@/components/Badge";
import { AssignmentsApi, ClassesApi } from "@/lib/resources";
import { Assignment, ClassRoom, Subject } from "@/types";
import { extractErrorMessage } from "@/lib/api";

function TeacherInner() {
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [classes, setClasses] = useState<ClassRoom[]>([]);
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [subjectId, setSubjectId] = useState<number | "">("");
  const [deadline, setDeadline] = useState("");
  const [maxMarks, setMaxMarks] = useState(100);
  const [allowResubmission, setAllowResubmission] = useState(true);
  const [publishNow, setPublishNow] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  async function refresh() {
    setLoading(true);
    try {
      const list = await AssignmentsApi.list();
      setAssignments(list);
      const cls = await ClassesApi.list();
      setClasses(cls);
      const subjectLists = await Promise.all(cls.map((c) => ClassesApi.subjects(c.id)));
      setSubjects(subjectLists.flat());
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
    if (subjectId === "") return;
    setError(null);
    setSubmitting(true);
    try {
      await AssignmentsApi.create({
        title,
        description,
        subjectId: Number(subjectId),
        deadline: new Date(deadline).toISOString(),
        maxMarks,
        allowResubmission,
        publishNow,
      });
      setTitle("");
      setDescription("");
      setDeadline("");
      setShowForm(false);
      await refresh();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar title="Teacher Dashboard" />
      <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-slate-900">My assignments</h2>
          <button
            onClick={() => setShowForm((s) => !s)}
            className="rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700"
          >
            {showForm ? "Cancel" : "+ New assignment"}
          </button>
        </div>

        {showForm && (
          <form
            onSubmit={handleCreate}
            className="mb-6 grid gap-3 rounded-lg border border-slate-200 bg-white p-4 sm:grid-cols-2"
          >
            <input
              required
              placeholder="Title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2"
            />
            <textarea
              required
              placeholder="Description / instructions"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2"
            />
            <select
              required
              value={subjectId}
              onChange={(e) => setSubjectId(e.target.value === "" ? "" : Number(e.target.value))}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            >
              <option value="">Select subject</option>
              {subjects.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.classRoomName})
                </option>
              ))}
            </select>
            <input
              required
              type="datetime-local"
              value={deadline}
              onChange={(e) => setDeadline(e.target.value)}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            />
            <input
              required
              type="number"
              min={1}
              max={1000}
              value={maxMarks}
              onChange={(e) => setMaxMarks(Number(e.target.value))}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
              placeholder="Max marks"
            />
            <div className="flex items-center gap-4 text-sm text-slate-600">
              <label className="flex items-center gap-1.5">
                <input
                  type="checkbox"
                  checked={allowResubmission}
                  onChange={(e) => setAllowResubmission(e.target.checked)}
                />
                Allow resubmission
              </label>
              <label className="flex items-center gap-1.5">
                <input
                  type="checkbox"
                  checked={publishNow}
                  onChange={(e) => setPublishNow(e.target.checked)}
                />
                Publish now
              </label>
            </div>
            {error && <p className="text-xs text-rose-600 sm:col-span-2">{error}</p>}
            <button
              type="submit"
              disabled={submitting}
              className="rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-60 sm:col-span-2"
            >
              {submitting ? "Creating…" : "Create assignment"}
            </button>
          </form>
        )}

        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-left text-xs font-medium uppercase text-slate-500">
              <tr>
                <th className="px-4 py-2">Title</th>
                <th className="px-4 py-2">Subject</th>
                <th className="px-4 py-2">Deadline</th>
                <th className="px-4 py-2">Status</th>
                <th className="px-4 py-2">Submissions</th>
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
              {!loading && assignments.length === 0 && (
                <tr>
                  <td className="px-4 py-4 text-slate-400" colSpan={6}>
                    No assignments yet.
                  </td>
                </tr>
              )}
              {assignments.map((a) => (
                <tr key={a.id}>
                  <td className="px-4 py-2 font-medium text-slate-900">{a.title}</td>
                  <td className="px-4 py-2 text-slate-600">
                    {a.subjectName} · {a.classRoomName}
                  </td>
                  <td className="px-4 py-2 text-slate-600">
                    {new Date(a.deadline).toLocaleString()}
                  </td>
                  <td className="px-4 py-2">
                    <Badge label={a.status} />
                  </td>
                  <td className="px-4 py-2 text-slate-600">{a.submissionCount}</td>
                  <td className="px-4 py-2 text-right">
                    <Link
                      href={`/teacher/assignments/${a.id}`}
                      className="text-xs font-medium text-indigo-600 hover:underline"
                    >
                      Manage →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </main>
    </div>
  );
}

export default function TeacherPage() {
  return (
    <RequireRole allow={["Teacher"]}>
      <TeacherInner />
    </RequireRole>
  );
}
