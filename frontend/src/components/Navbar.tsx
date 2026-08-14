"use client";

import { useAuth } from "@/lib/auth-context";

export default function Navbar({ title }: { title: string }) {
  const { user, logout } = useAuth();

  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3 sm:px-6">
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-indigo-600">
            Assignment &amp; Submission System
          </p>
          <h1 className="text-lg font-semibold text-slate-900">{title}</h1>
        </div>
        {user && (
          <div className="flex items-center gap-3">
            <div className="text-right text-sm">
              <p className="font-medium text-slate-900">{user.fullName}</p>
              <p className="text-slate-500">{user.role}</p>
            </div>
            <button
              onClick={logout}
              className="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Sign out
            </button>
          </div>
        )}
      </div>
    </header>
  );
}
