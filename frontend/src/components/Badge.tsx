const styles: Record<string, string> = {
  Draft: "bg-slate-100 text-slate-700",
  Published: "bg-emerald-100 text-emerald-700",
  Closed: "bg-slate-200 text-slate-600",
  Submitted: "bg-blue-100 text-blue-700",
  Resubmitted: "bg-indigo-100 text-indigo-700",
  Late: "bg-amber-100 text-amber-700",
  Graded: "bg-emerald-100 text-emerald-700",
  Returned: "bg-rose-100 text-rose-700",
};

export default function Badge({ label }: { label: string }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
        styles[label] ?? "bg-slate-100 text-slate-700"
      }`}
    >
      {label}
    </span>
  );
}
