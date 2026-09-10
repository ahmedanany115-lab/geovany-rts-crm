"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { useRoles } from "@/hooks/useRoles";
import { useState } from "react";
import { HeartHandshake, Plus, X, CheckCircle, XCircle, Trash2, RefreshCw, ChevronDown } from "lucide-react";

interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeEmail: string;
  employeeName: string;
  type: string;
  typeId: number;
  startDate: string;
  endDate: string;
  daysCount: number;
  reason: string;
  status: string;
  statusId: number;
  reviewedByName?: string;
  reviewedAt?: string;
  reviewNote?: string;
  createdAt: string;
}

const STATUS_COLORS: Record<string, string> = {
  Pending:  "bg-amber-100 text-amber-700",
  Approved: "bg-emerald-100 text-emerald-700",
  Rejected: "bg-red-100 text-red-700",
};

const LEAVE_TYPES = [
  { id: 1, label_en: "Vacation",    label_ar: "إجازة سنوية" },
  { id: 2, label_en: "Sick Leave",  label_ar: "إجازة مرضية" },
  { id: 3, label_en: "Permission",  label_ar: "إذن" },
  { id: 4, label_en: "Emergency",   label_ar: "طارئ" },
];

export default function LeavesPage() {
  const { t, lang } = useT();
  const { isFinance, isAdmin } = useRoles();
  const canReview = isFinance || isAdmin;
  const qc = useQueryClient();

  const [showForm, setShowForm] = useState(false);
  const [tab, setTab] = useState<"mine" | "all">("mine");
  const [reviewingId, setReviewingId] = useState<string | null>(null);
  const [reviewNote, setReviewNote] = useState("");
  const [form, setForm] = useState({ type: 1, startDate: "", endDate: "", reason: "" });

  const { data: leaves, isLoading, refetch } = useQuery({
    queryKey: ["leaves", tab],
    queryFn: () => apiFetch<LeaveRequest[]>(`/hr/leaves`),
  });

  const myLeaves  = leaves?.filter(l => tab === "mine" ? true : true) ?? [];
  // Backend already filters by ownership for non-reviewers;
  // for reviewers, "mine" tab is not shown

  const submit = useMutation({
    mutationFn: (data: typeof form) =>
      apiFetch("/hr/leaves", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["leaves"] });
      setForm({ type: 1, startDate: "", endDate: "", reason: "" });
      setShowForm(false);
    },
  });

  const review = useMutation({
    mutationFn: ({ id, approve, note }: { id: string; approve: boolean; note: string }) =>
      apiFetch(`/hr/leaves/${id}/review`, { method: "PATCH", body: JSON.stringify({ approve, note }) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["leaves"] });
      setReviewingId(null);
      setReviewNote("");
    },
  });

  const remove = useMutation({
    mutationFn: (id: string) => apiFetch(`/hr/leaves/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["leaves"] }),
  });

  const days = form.startDate && form.endDate
    ? Math.max(0, Math.floor((new Date(form.endDate).getTime() - new Date(form.startDate).getTime()) / 86400000) + 1)
    : 0;

  return (
    <div className="p-6 space-y-6 max-w-5xl">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <HeartHandshake className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">{t("leaves")}</h1>
            <p className="text-sm text-muted-foreground">
              {canReview ? t("hr_approval") : t("my_requests")}
            </p>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
            <RefreshCw className="h-4 w-4" />
          </button>
          <button
            onClick={() => setShowForm(v => !v)}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"
          >
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel_request") : t("new_leave")}
          </button>
        </div>
      </div>

      {/* New leave form */}
      {showForm && (
        <form
          onSubmit={e => { e.preventDefault(); submit.mutate(form); }}
          className="card p-5 space-y-4 border border-primary/20"
        >
          <h2 className="font-semibold">{t("new_leave")}</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("leave_type")} *</label>
              <select
                value={form.type}
                onChange={e => setForm(f => ({ ...f, type: Number(e.target.value) }))}
                className="input w-full"
              >
                {LEAVE_TYPES.map(lt => (
                  <option key={lt.id} value={lt.id}>
                    {lang === "ar" ? lt.label_ar : lt.label_en}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex gap-2 col-span-2 md:col-span-1">
              <div className="flex-1">
                <label className="text-xs text-muted-foreground block mb-1">{t("start_date")} *</label>
                <input
                  required type="date"
                  value={form.startDate}
                  onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div className="flex-1">
                <label className="text-xs text-muted-foreground block mb-1">{t("end_date")} *</label>
                <input
                  required type="date"
                  value={form.endDate}
                  min={form.startDate}
                  onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))}
                  className="input w-full"
                />
              </div>
              {days > 0 && (
                <div className="flex items-end pb-1">
                  <span className="text-sm text-muted-foreground whitespace-nowrap">{days} {t("days_count")}</span>
                </div>
              )}
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">{t("reason")} *</label>
              <textarea
                required rows={2}
                value={form.reason}
                onChange={e => setForm(f => ({ ...f, reason: e.target.value }))}
                className="input w-full resize-none"
                placeholder={lang === "ar" ? "اذكر سبب الإجازة..." : "Briefly describe your reason..."}
              />
            </div>
          </div>
          <button
            type="submit"
            disabled={submit.isPending}
            className="btn-primary px-4 py-2 rounded-lg text-sm"
          >
            {submit.isPending ? t("saving") : t("submit_request")}
          </button>
          {submit.isError && (
            <p className="text-sm text-red-600">Failed to submit — please try again.</p>
          )}
        </form>
      )}

      {/* Tabs — reviewers can see all */}
      {canReview && (
        <div className="flex gap-1 border-b border-border">
          {(["mine", "all"] as const).map(k => (
            <button
              key={k}
              onClick={() => setTab(k)}
              className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
                tab === k ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"
              }`}
            >
              {k === "mine" ? t("my_requests") : t("all_requests")}
            </button>
          ))}
        </div>
      )}

      {/* Requests table */}
      {isLoading ? (
        <div className="text-center py-12 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="space-y-3">
          {myLeaves.length === 0 && (
            <div className="card p-8 text-center text-muted-foreground">{t("no_data")}</div>
          )}
          {myLeaves.map(req => (
            <div key={req.id} className="card p-4 space-y-3">
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1 min-w-0">
                  {/* Show employee name for reviewers */}
                  {canReview && (
                    <p className="text-xs text-muted-foreground mb-0.5">
                      {req.employeeName || req.employeeEmail}
                    </p>
                  )}
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="font-medium text-sm">
                      {LEAVE_TYPES.find(lt => lt.id === req.typeId)?.[lang === "ar" ? "label_ar" : "label_en"] ?? req.type}
                    </span>
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${STATUS_COLORS[req.status] ?? "bg-muted"}`}>
                      {lang === "ar"
                        ? req.status === "Pending" ? "قيد الانتظار" : req.status === "Approved" ? "معتمد" : "مرفوض"
                        : req.status}
                    </span>
                    <span className="text-xs text-muted-foreground">
                      {req.startDate} → {req.endDate} ({req.daysCount} {t("days_count")})
                    </span>
                  </div>
                  {req.reason && (
                    <p className="text-sm text-muted-foreground mt-1 truncate">{req.reason}</p>
                  )}
                  {req.reviewedByName && (
                    <p className="text-xs text-muted-foreground mt-1">
                      {t("reviewed_by")}: {req.reviewedByName}
                      {req.reviewNote ? ` — "${req.reviewNote}"` : ""}
                    </p>
                  )}
                </div>

                {/* Actions */}
                <div className="flex items-center gap-1 shrink-0">
                  {/* HR review buttons */}
                  {canReview && req.statusId === 1 && (
                    reviewingId === req.id ? (
                      <div className="flex flex-col gap-2 min-w-[200px]">
                        <input
                          value={reviewNote}
                          onChange={e => setReviewNote(e.target.value)}
                          placeholder={t("review_note")}
                          className="input text-xs py-1"
                        />
                        <div className="flex gap-1">
                          <button
                            onClick={() => review.mutate({ id: req.id, approve: true, note: reviewNote })}
                            disabled={review.isPending}
                            className="flex-1 flex items-center justify-center gap-1 text-xs px-2 py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50"
                          >
                            <CheckCircle className="h-3.5 w-3.5" />{t("approve")}
                          </button>
                          <button
                            onClick={() => review.mutate({ id: req.id, approve: false, note: reviewNote })}
                            disabled={review.isPending}
                            className="flex-1 flex items-center justify-center gap-1 text-xs px-2 py-1.5 rounded bg-red-600 text-white hover:bg-red-700 disabled:opacity-50"
                          >
                            <XCircle className="h-3.5 w-3.5" />{t("reject")}
                          </button>
                          <button
                            onClick={() => { setReviewingId(null); setReviewNote(""); }}
                            className="px-2 py-1.5 rounded hover:bg-accent text-xs"
                          >
                            <X className="h-3.5 w-3.5" />
                          </button>
                        </div>
                      </div>
                    ) : (
                      <button
                        onClick={() => setReviewingId(req.id)}
                        className="flex items-center gap-1 text-xs px-3 py-1.5 rounded-lg bg-primary/10 text-primary hover:bg-primary/20 font-medium"
                      >
                        {t("hr_approval")} <ChevronDown className="h-3 w-3" />
                      </button>
                    )
                  )}

                  {/* Cancel/delete — own pending requests */}
                  {req.statusId === 1 && (
                    <button
                      onClick={() => remove.mutate(req.id)}
                      disabled={remove.isPending}
                      title="Cancel request"
                      className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
