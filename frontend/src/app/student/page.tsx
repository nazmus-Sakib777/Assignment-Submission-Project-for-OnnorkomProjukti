"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import RequireRole from "@/components/RequireRole";
import Navbar from "@/components/Navbar";
import Badge from "@/components/Badge";
import { AssignmentsApi } from "@/lib/resources";
import { Assignment } from "@/types";
import { extractErrorMessage } from "@/lib/api";

function StudentInner() {
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    AssignmentsApi.list()
      .then(setAssignments)
      .catch((err) => setError(extractErrorMessage(err)))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar title="Student Dashboard" />
      <main className="mx-auto max-w-4xl px-4 py-6 sm:px-6">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">Your assignments</h2>
        {error && <p className="mb-3 text-sm text-rose-600">{error}</p>}
        {loading && <p className="text-sm text-slate-400">Loading…</p>}

        <div className="space-y-3">
          {!loading && assignments.length === 0 && (
            <p className="text-sm text-slate-400">No assignments published yet.</p>
          )}
          {assignments.map((a) => (
            <Link
              key={a.id}
              href={`/student/assignments/${a.id}`}
              className="block rounded-lg border border-slate-200 bg-white p-4 hover:border-indigo-300"
            >
              <div className="flex items-start justify-between">
                <div>
                  <p className="font-medium text-slate-900">{a.title}</p>
                  <p className="text-xs text-slate-500">{a.subjectName}</p>
                </div>
                {a.mySubmission ? (
                  <Badge label={a.mySubmission.status} />
                ) : (
                  <Badge label="Not submitted" />
                )}
              </div>
              <div className="mt-2 flex flex-wrap gap-4 text-xs text-slate-500">
                <span>Deadline: {new Date(a.deadline).toLocaleString()}</span>
                <span>Max marks: {a.maxMarks}</span>
                {a.mySubmission?.marks !== null && a.mySubmission?.marks !== undefined && (
                  <span className="font-medium text-emerald-700">
                    Marks: {a.mySubmission.marks}/{a.maxMarks}
                  </span>
                )}
              </div>
            </Link>
          ))}
        </div>
      </main>
    </div>
  );
}

export default function StudentPage() {
  return (
    <RequireRole allow={["Student"]}>
      <StudentInner />
    </RequireRole>
  );
}
