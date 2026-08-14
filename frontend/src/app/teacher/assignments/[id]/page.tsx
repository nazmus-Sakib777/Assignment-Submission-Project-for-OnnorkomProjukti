"use client";

import { FormEvent, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import RequireRole from "@/components/RequireRole";
import Navbar from "@/components/Navbar";
import Badge from "@/components/Badge";
import { AssignmentsApi, SubmissionsApi } from "@/lib/resources";
import { Assignment, Submission } from "@/types";
import { extractErrorMessage } from "@/lib/api";

function TeacherAssignmentInner() {
  const params = useParams<{ id: string }>();
  const assignmentId = Number(params.id);

  const [assignment, setAssignment] = useState<Assignment | null>(null);
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [gradingId, setGradingId] = useState<number | null>(null);
  const [marks, setMarks] = useState("");
  const [feedback, setFeedback] = useState("");
  const [saving, setSaving] = useState(false);

  async function refresh() {
    setLoading(true);
    try {
      const [a, subs] = await Promise.all([
        AssignmentsApi.get(assignmentId),
        SubmissionsApi.forAssignment(assignmentId),
      ]);
      setAssignment(a);
      setSubmissions(subs);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [assignmentId]);

  function startGrading(s: Submission) {
    setGradingId(s.id);
    setMarks(s.marks?.toString() ?? "");
    setFeedback(s.teacherFeedback ?? "");
  }

  async function handleGrade(e: FormEvent) {
    e.preventDefault();
    if (gradingId === null) return;
    setError(null);
    setSaving(true);
    try {
      await SubmissionsApi.grade(gradingId, Number(marks), feedback);
      setGradingId(null);
      await refresh();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function togglePublish() {
    if (!assignment) return;
    try {
      if (assignment.status === "Draft") await AssignmentsApi.publish(assignment.id);
      else if (assignment.status === "Published") await AssignmentsApi.close(assignment.id);
      await refresh();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  if (loading || !assignment) {
    return (
      <div className="min-h-screen bg-slate-50">
        <Navbar title="Assignment" />
        <main className="mx-auto max-w-4xl px-4 py-6 text-slate-400">Loading…</main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar title="Manage Assignment" />
      <main className="mx-auto max-w-4xl px-4 py-6 sm:px-6">
        <div className="rounded-lg border border-slate-200 bg-white p-5">
          <div className="flex items-start justify-between">
            <div>
              <h2 className="text-lg font-semibold text-slate-900">{assignment.title}</h2>
              <p className="text-sm text-slate-500">
                {assignment.subjectName} · {assignment.classRoomName}
              </p>
            </div>
            <Badge label={assignment.status} />
          </div>
          <p className="mt-3 whitespace-pre-wrap text-sm text-slate-700">{assignment.description}</p>
          <div className="mt-3 flex flex-wrap gap-4 text-xs text-slate-500">
            <span>Deadline: {new Date(assignment.deadline).toLocaleString()}</span>
            <span>Max marks: {assignment.maxMarks}</span>
            <span>Resubmission: {assignment.allowResubmission ? "Allowed" : "Not allowed"}</span>
          </div>
          {assignment.status !== "Closed" && (
            <button
              onClick={togglePublish}
              className="mt-4 rounded-md border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50"
            >
              {assignment.status === "Draft" ? "Publish" : "Close submissions"}
            </button>
          )}
        </div>

        <h3 className="mb-3 mt-6 text-sm font-semibold text-slate-900">
          Submissions ({submissions.length})
        </h3>
        {error && <p className="mb-3 text-sm text-rose-600">{error}</p>}

        <div className="space-y-3">
          {submissions.length === 0 && (
            <p className="text-sm text-slate-400">No submissions yet.</p>
          )}
          {submissions.map((s) => (
            <div key={s.id} className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-sm font-medium text-slate-900">{s.studentName}</p>
                  <p className="text-xs text-slate-500">
                    Submitted {new Date(s.submittedAt).toLocaleString()}
                    {s.updatedAt && ` · updated ${new Date(s.updatedAt).toLocaleString()}`}
                  </p>
                </div>
                <Badge label={s.status} />
              </div>
              <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700">{s.answerText}</p>
              {s.attachmentUrl && (
                <a
                  href={s.attachmentUrl}
                  target="_blank"
                  className="mt-1 inline-block text-xs text-indigo-600 hover:underline"
                >
                  View attachment
                </a>
              )}

              {s.marks !== null && (
                <p className="mt-2 text-sm font-medium text-emerald-700">
                  Marks: {s.marks} / {s.maxMarks}
                </p>
              )}
              {s.teacherFeedback && (
                <p className="mt-1 text-sm text-slate-600">Feedback: {s.teacherFeedback}</p>
              )}

              {gradingId === s.id ? (
                <form onSubmit={handleGrade} className="mt-3 space-y-2 border-t border-slate-100 pt-3">
                  <input
                    required
                    type="number"
                    step="0.5"
                    min={0}
                    max={s.maxMarks ?? 100}
                    value={marks}
                    onChange={(e) => setMarks(e.target.value)}
                    placeholder={`Marks (0-${s.maxMarks})`}
                    className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                  />
                  <textarea
                    value={feedback}
                    onChange={(e) => setFeedback(e.target.value)}
                    placeholder="Feedback (optional)"
                    rows={2}
                    className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                  />
                  <div className="flex gap-2">
                    <button
                      type="submit"
                      disabled={saving}
                      className="rounded-md bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-indigo-700 disabled:opacity-60"
                    >
                      {saving ? "Saving…" : "Save grade"}
                    </button>
                    <button
                      type="button"
                      onClick={() => setGradingId(null)}
                      className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-700"
                    >
                      Cancel
                    </button>
                  </div>
                </form>
              ) : (
                <button
                  onClick={() => startGrading(s)}
                  className="mt-3 text-xs font-medium text-indigo-600 hover:underline"
                >
                  {s.marks !== null ? "Update grade" : "Grade submission"}
                </button>
              )}
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}

export default function TeacherAssignmentPage() {
  return (
    <RequireRole allow={["Teacher"]}>
      <TeacherAssignmentInner />
    </RequireRole>
  );
}
