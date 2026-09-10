"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { useRoles } from "@/hooks/useRoles";
import { useState } from "react";
import { CalendarDays, Plus, X, Trash2, RefreshCw, Clock } from "lucide-react";

interface MeetingLog {
  id: string;
  employeeId: string;
  employeeEmail: string;
  employeeName: string;
  title: string;
  description?: string;
  type: string;
  typeId: number;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  location?: string;
  attendees?: string;
  outcome?: string;
  createdAt: string;
}

const MEETING_TYPES = [
  { id: 1, label_en: "Internal Meeting", label_ar: "اجتماع داخلي" },
  { id: 2, label_en: "Client Visit",     label_ar: "زيارة عميل" },
  { id: 3, label_en: "Site Visit",       label_ar: "زيارة موقع" },
  { id: 4, label_en: "Training",         label_ar: "تدريب" },
  { id: 5, label_en: "Other",            label_ar: "أخرى" },
];

const TYPE_COLORS: Record<number, string> = {
  1: "bg-blue-100 text-blue-700",
  2: "bg-emerald-100 text-emerald-700",
  3: "bg-orange-100 text-orange-700",
  4: "bg-purple-100 text-purple-700",
  5: "bg-gray-100 text-gray-700",
};

function fmt(dateStr: string) {
  const d = new Date(dateStr);
  return d.toLocaleString("en-EG", { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
}

export default function MeetingsPage() {
  const { t, lang } = useT();
  const { isFinance, isAdmin } = useRoles();
  const canViewAll = isFinance || isAdmin;
  const qc = useQueryClient();

  const [showForm, setShowForm] = useState(false);
  const [tab, setTab] = useState<"mine" | "all">("mine");
  const [form, setForm] = useState({
    title: "", description: "", type: 1,
    startTime: "", endTime: "",
    location: "", attendees: "", outcome: "",
  });

  const { data: meetings, isLoading, refetch } = useQuery({
    queryKey: ["meetings", tab],
    queryFn: () => apiFetch<MeetingLog[]>(`/hr/meetings`),
  });

  const submit = useMutation({
    mutationFn: (data: typeof form) =>
      apiFetch("/hr/meetings", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["meetings"] });
      setForm({ title: "", description: "", type: 1, startTime: "", endTime: "", location: "", attendees: "", outcome: "" });
      setShowForm(false);
    },
  });

  const remove = useMutation({
    mutationFn: (id: string) => apiFetch(`/hr/meetings/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["meetings"] }),
  });

  const durationMins = form.startTime && form.endTime
    ? Math.max(0, Math.round((new Date(form.endTime).getTime() - new Date(form.startTime).getTime()) / 60000))
    : 0;

  return (
    <div className="p-6 space-y-6 max-w-5xl">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <CalendarDays className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">{t("meetings")}</h1>
            <p className="text-sm text-muted-foreground">{t("my_requests")}</p>
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
            {showForm ? t("cancel") : t("new_meeting")}
          </button>
        </div>
      </div>

      {/* Form */}
      {showForm && (
        <form
          onSubmit={e => { e.preventDefault(); submit.mutate(form); }}
          className="card p-5 space-y-4 border border-primary/20"
        >
          <h2 className="font-semibold">{t("new_meeting")}</h2>
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">{t("meeting_title")} *</label>
              <input
                required value={form.title}
                onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                className="input w-full"
                placeholder={lang === "ar" ? "عنوان الاجتماع..." : "Meeting title..."}
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("meeting_type")} *</label>
              <select
                value={form.type}
                onChange={e => setForm(f => ({ ...f, type: Number(e.target.value) }))}
                className="input w-full"
              >
                {MEETING_TYPES.map(mt => (
                  <option key={mt.id} value={mt.id}>
                    {lang === "ar" ? mt.label_ar : mt.label_en}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("location")}</label>
              <input
                value={form.location}
                onChange={e => setForm(f => ({ ...f, location: e.target.value }))}
                className="input w-full"
                placeholder={lang === "ar" ? "الموقع..." : "Office / Client site..."}
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("start_time")} *</label>
              <input
                required type="datetime-local"
                value={form.startTime}
                onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))}
                className="input w-full"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("end_time")} *</label>
              <input
                required type="datetime-local"
                value={form.endTime}
                min={form.startTime}
                onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))}
                className="input w-full"
              />
              {durationMins > 0 && (
                <p className="text-xs text-muted-foreground mt-1">
                  <Clock className="h-3 w-3 inline mr-1" />
                  {durationMins >= 60
                    ? `${Math.floor(durationMins/60)}h ${durationMins%60}m`
                    : `${durationMins}m`}
                </p>
              )}
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">{t("attendees")}</label>
              <input
                value={form.attendees}
                onChange={e => setForm(f => ({ ...f, attendees: e.target.value }))}
                className="input w-full"
                placeholder={lang === "ar" ? "أسماء الحضور مفصولة بفاصلة..." : "Names separated by commas..."}
              />
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">{t("outcome")}</label>
              <textarea
                rows={2}
                value={form.outcome}
                onChange={e => setForm(f => ({ ...f, outcome: e.target.value }))}
                className="input w-full resize-none"
                placeholder={lang === "ar" ? "نتائج الاجتماع..." : "Key outcomes, decisions, action items..."}
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
        </form>
      )}

      {/* Tabs */}
      {canViewAll && (
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

      {/* List */}
      {isLoading ? (
        <div className="text-center py-12 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="space-y-3">
          {meetings?.length === 0 && (
            <div className="card p-8 text-center text-muted-foreground">{t("no_data")}</div>
          )}
          {meetings?.map(m => {
            const mt = MEETING_TYPES.find(x => x.id === m.typeId);
            return (
              <div key={m.id} className="card p-4">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1 min-w-0">
                    {canViewAll && (
                      <p className="text-xs text-muted-foreground mb-0.5">
                        {m.employeeName || m.employeeEmail}
                      </p>
                    )}
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-medium text-sm">{m.title}</span>
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${TYPE_COLORS[m.typeId] ?? "bg-muted"}`}>
                        {mt ? (lang === "ar" ? mt.label_ar : mt.label_en) : m.type}
                      </span>
                    </div>
                    <div className="flex items-center gap-3 mt-1 text-xs text-muted-foreground flex-wrap">
                      <span>{fmt(m.startTime)} → {fmt(m.endTime)}</span>
                      <span className="flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        {m.durationMinutes >= 60
                          ? `${Math.floor(m.durationMinutes/60)}h ${m.durationMinutes%60}m`
                          : `${m.durationMinutes}m`}
                      </span>
                      {m.location && <span>📍 {m.location}</span>}
                    </div>
                    {m.attendees && (
                      <p className="text-xs text-muted-foreground mt-0.5">{t("attendees")}: {m.attendees}</p>
                    )}
                    {m.outcome && (
                      <p className="text-sm text-foreground/80 mt-1 line-clamp-2">{m.outcome}</p>
                    )}
                  </div>
                  <button
                    onClick={() => remove.mutate(m.id)}
                    disabled={remove.isPending}
                    className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 shrink-0"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
