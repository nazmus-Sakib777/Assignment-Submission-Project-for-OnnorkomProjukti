"use client";

import { FormEvent, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import RequireRole from "@/components/RequireRole";
import Navbar from "@/components/Navbar";
import Badge from "@/components/Badge";
import { AssignmentsApi, SubmissionsApi } from "@/lib/resources";
import { Assignment } from "@/types";
import { extractErrorMessage } from "@/lib/api";

function StudentAssignmentInner() {
  const params = useParams<{ id: string }>();
  const assignmentId = Number(params.id);

  const [assignment, setAssignment] = useState<Assignment | null>(null);
  const [answerText, setAnswerText] = useState("");
  const [attachmentUrl, setAttachmentUrl] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function refresh() {
    setLoading(true);
    try {
      const a = await AssignmentsApi.get(assignmentId);
      setAssignment(a);
      if (a.mySubmission) {
        setAnswerText(a.mySubmission.answerText);
        setAttachmentUrl(a.mySubmission.attachmentUrl ?? "");
      }
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

  const isPastDeadline = assignment ? new Date(assignment.deadline) < new Date() : false;
  const hasSubmission = !!assignment?.mySubmission;
  const canEdit =
    assignment &&
    assignment.status === "Published" &&
    !isPastDeadline &&
    (!hasSubmission || assignment.allowResubmission);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!assignment) return;
    setError(null);
    setSuccess(null);
    setSubmitting(true);
    try {
      if (hasSubmission) {
        await SubmissionsApi.update(assignment.mySubmission!.id, answerText, attachmentUrl || undefined);
        setSuccess("Submission updated.");
      } else {
        await SubmissionsApi.submit(assignment.id, answerText, attachmentUrl || undefined);
        setSuccess("Submitted successfully.");
      }
      await refresh();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  if (loading || !assignment) {
    return (
      <div className="min-h-screen bg-slate-50">
        <Navbar title="Assignment" />
        <main className="mx-auto max-w-3xl px-4 py-6 text-slate-400">Loading…</main>
      </div>
    );
  }

  const sub = assignment.mySubmission;

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar title="Assignment" />
      <main className="mx-auto max-w-3xl px-4 py-6 sm:px-6">
        <div className="rounded-lg border border-slate-200 bg-white p-5">
          <div className="flex items-start justify-between">
            <div>
              <h2 className="text-lg font-semibold text-slate-900">{assignment.title}</h2>
              <p className="text-sm text-slate-500">
                {assignment.subjectName} · {assignment.createdByTeacherName}
              </p>
            </div>
            {sub ? <Badge label={sub.status} /> : <Badge label="Not submitted" />}
          </div>
          <p className="mt-3 whitespace-pre-wrap text-sm text-slate-700">{assignment.description}</p>
          <div className="mt-3 flex flex-wrap gap-4 text-xs text-slate-500">
            <span>Deadline: {new Date(assignment.deadline).toLocaleString()}</span>
            <span>Max marks: {assignment.maxMarks}</span>
          </div>
          {sub?.marks !== null && sub?.marks !== undefined && (
            <div className="mt-3 rounded-md bg-emerald-50 p-3 text-sm">
              <p className="font-medium text-emerald-800">
                Marks: {sub.marks} / {assignment.maxMarks}
              </p>
              {sub.teacherFeedback && (
                <p className="mt-1 text-emerald-700">Feedback: {sub.teacherFeedback}</p>
              )}
            </div>
          )}
        </div>

        <div className="mt-6 rounded-lg border border-slate-200 bg-white p-5">
          <h3 className="mb-3 text-sm font-semibold text-slate-900">
            {hasSubmission ? "Your submission" : "Submit your answer"}
          </h3>

          {!canEdit && (
            <p className="mb-3 rounded-md bg-slate-50 px-3 py-2 text-xs text-slate-500">
              {isPastDeadline
                ? "The deadline has passed — this can no longer be edited."
                : hasSubmission && !assignment.allowResubmission
                ? "Resubmission is not allowed for this assignment."
                : assignment.status !== "Published"
                ? "This assignment is not currently open for submission."
                : ""}
            </p>
          )}

          <form onSubmit={handleSubmit} className="space-y-3">
            <textarea
              required
              disabled={!canEdit}
              value={answerText}
              onChange={(e) => setAnswerText(e.target.value)}
              rows={8}
              placeholder="Write your answer here…"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50 disabled:text-slate-500"
            />
            <input
              disabled={!canEdit}
              value={attachmentUrl}
              onChange={(e) => setAttachmentUrl(e.target.value)}
              placeholder="Attachment URL (optional)"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50 disabled:text-slate-500"
            />
            {error && <p className="text-sm text-rose-600">{error}</p>}
            {success && <p className="text-sm text-emerald-600">{success}</p>}
            {canEdit && (
              <button
                type="submit"
                disabled={submitting}
                className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-60"
              >
                {submitting ? "Saving…" : hasSubmission ? "Update submission" : "Submit"}
              </button>
            )}
          </form>
        </div>
      </main>
    </div>
  );
}

export default function StudentAssignmentPage() {
  return (
    <RequireRole allow={["Student"]}>
      <StudentAssignmentInner />
    </RequireRole>
  );
}
