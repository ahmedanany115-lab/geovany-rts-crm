"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { useRoles } from "@/hooks/useRoles";
import {
  FileText, Plus, X, Trash2, Pencil, RefreshCw,
  Search, AlertTriangle, Check, Download, ExternalLink,
} from "lucide-react";

/* ── Category colours ────────────────────────────────────────────────────── */
const CATEGORIES = [
  "Company Registration","Tax Card","VAT Certificate","Commercial Registration",
  "Contracts","Bank Documents","Certifications","Partner Certificates",
  "Technical Documents","Other",
];

const CAT_COLORS: Record<string, string> = {
  "Company Registration": "bg-blue-100 text-blue-700",
  "Tax Card":             "bg-amber-100 text-amber-700",
  "VAT Certificate":      "bg-purple-100 text-purple-700",
  "Commercial Registration": "bg-emerald-100 text-emerald-700",
  "Contracts":            "bg-rose-100 text-rose-700",
  "Bank Documents":       "bg-cyan-100 text-cyan-700",
};

const INIT = {
  name: "", category: "Company Registration", description: "",
  documentNumber: "", expiryDate: "", isPublic: true,
  storagePath: "", uploadedByName: "",
};

/* ── Permission helpers ──────────────────────────────────────────────────── */
// Upload/Edit: Admin, Manager, Accountant, Marketing (matches backend roles)
// Delete:      Admin, Manager only
// Download:    ALL authenticated users — no role check needed

export default function DocumentsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const qc = useQueryClient();
  const { isAdmin, hasRole } = useRoles();

  // Permission flags — must match backend [Authorize(Roles = ...)]
  const canUpload = isAdmin || hasRole("Manager", "Accountant", "Marketing");
  const canEdit   = canUpload;                                  // same set
  const canDelete = isAdmin || hasRole("Manager");             // tighter set
  // canDownload = true for ALL authenticated users (no role check)

  const [search,    setSearch]    = useState("");
  const [catFilter, setCat]       = useState("");
  const [showForm,  setShowForm]  = useState(false);
  const [editing,   setEditing]   = useState<any>(null);
  const [delTarget, setDelTarget] = useState<any>(null);
  const [form,      setForm]      = useState(INIT);

  /* ── Data ── */
  const { data: docs, isLoading, refetch } = useQuery({
    queryKey: ["documents", catFilter],
    queryFn:  () => apiFetch<any[]>(
      `/documents${catFilter ? `?category=${encodeURIComponent(catFilter)}` : ""}`
    ),
  });

  /* ── Mutations ── */
  const createMut = useMutation({
    mutationFn: (data: any) =>
      apiFetch<any>("/documents", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["documents"] });
      toast("Document saved.", "success");
      closeForm();
    },
    onError: (e: any) => toast(e?.message ?? "Failed to save.", "error"),
  });

  const updateMut = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) =>
      apiFetch<void>(`/documents/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["documents"] });
      toast("Updated.", "success");
      closeForm();
    },
    onError: (e: any) => toast(e?.message ?? "Failed.", "error"),
  });

  const delMut = useMutation({
    mutationFn: (id: string) =>
      apiFetch<void>(`/documents/${id}`, { method: "DELETE" }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["documents"] });
      toast("Deleted.", "info");
      setDelTarget(null);
    },
    onError: (e: any) => toast(e?.message ?? "Failed.", "error"),
  });

  /* ── Form helpers ── */
  const openEdit = (doc: any) => {
    setEditing(doc);
    setForm({
      name: doc.name, category: doc.category,
      description: doc.description ?? "", documentNumber: doc.documentNumber ?? "",
      expiryDate: doc.expiryDate ?? "", isPublic: doc.isPublic,
      storagePath: doc.storagePath ?? "", uploadedByName: doc.uploadedByName ?? "",
    });
    setShowForm(true);
  };

  const closeForm = () => { setShowForm(false); setEditing(null); setForm(INIT); };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      ...form,
      expiryDate:    form.expiryDate || null,
      fileSizeBytes: 0,
      fileName:      form.name,
      originalName:  form.name,
      contentType:   "application/octet-stream",
    };
    if (editing) updateMut.mutate({ id: editing.id, data: payload });
    else          createMut.mutate(payload);
  };

  /* ── Download handler — calls auth-gated backend endpoint ── */
  const handleDownload = async (doc: any) => {
    try {
      // If storagePath is an absolute URL, open it directly via the download endpoint
      // The endpoint will redirect for URLs and stream local files — both are auth-gated.
      const apiBase = (process.env.NEXT_PUBLIC_API_URL ?? "").replace(/\/$/, "");
      const downloadUrl = `${apiBase}/api/v1/documents/${doc.id}/download`;

      // Open in new tab — the browser will follow the redirect if it's an external URL.
      // For binary files the browser will trigger a download.
      window.open(downloadUrl, "_blank", "noopener,noreferrer");
    } catch {
      toast("Failed to open document.", "error");
    }
  };

  /* ── Filtered list ── */
  const filtered = (docs ?? []).filter(d =>
    !search
      || d.name.toLowerCase().includes(search.toLowerCase())
      || d.category.toLowerCase().includes(search.toLowerCase())
      || (d.description ?? "").toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="p-6 space-y-6">

      {/* ── Delete confirm modal ── */}
      {delTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-red-600">
              <AlertTriangle className="h-5 w-5" />
              <h2 className="font-semibold">Delete Document?</h2>
            </div>
            <p className="text-sm text-muted-foreground">
              Delete <strong>{delTarget.name}</strong>? This cannot be undone.
            </p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setDelTarget(null)}
                className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={() => delMut.mutate(delTarget.id)}
                disabled={delMut.isPending}
                className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">
                {delMut.isPending ? "Deleting…" : "Delete"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <FileText className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">Company Documents</h1>
            <p className="text-sm text-muted-foreground">
              {canUpload
                ? "Upload, manage, and share company documents"
                : "Browse and download company documents"}
            </p>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
            <RefreshCw className="h-4 w-4" />
          </button>
          {/* Upload button — only for permitted roles */}
          {canUpload && (
            <button
              onClick={showForm ? closeForm : () => setShowForm(true)}
              className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"
            >
              {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
              {showForm ? t("cancel") : "Add Document"}
            </button>
          )}
        </div>
      </div>

      {/* ── Upload / Edit form (canUpload / canEdit only) ── */}
      {showForm && canUpload && (
        <form onSubmit={handleSubmit}
          className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">
            {editing ? `Edit: ${editing.name}` : "Add New Document"}
          </h2>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Name *</label>
              <input required value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                className="input w-full" />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Category *</label>
              <select value={form.category}
                onChange={e => setForm(f => ({ ...f, category: e.target.value }))}
                className="input w-full">
                {CATEGORIES.map(c => <option key={c}>{c}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Document #</label>
              <input value={form.documentNumber}
                onChange={e => setForm(f => ({ ...f, documentNumber: e.target.value }))}
                className="input w-full" />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Expiry Date</label>
              <input type="date" value={form.expiryDate}
                onChange={e => setForm(f => ({ ...f, expiryDate: e.target.value }))}
                className="input w-full" />
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Description</label>
              <textarea rows={2} value={form.description}
                onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                className="input w-full resize-none" />
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">
                File URL / Reference Path
              </label>
              <input value={form.storagePath}
                onChange={e => setForm(f => ({ ...f, storagePath: e.target.value }))}
                className="input w-full"
                placeholder="https://drive.example.com/... or /uploads/filename.pdf" />
              <p className="text-xs text-muted-foreground mt-1">
                Paste a Google Drive / SharePoint link, or an internal file path.
                Download is always auth-gated — only company users can access it.
              </p>
            </div>
            <div className="flex items-center gap-2">
              <input type="checkbox" id="isPublic" checked={form.isPublic}
                onChange={e => setForm(f => ({ ...f, isPublic: e.target.checked }))} />
              <label htmlFor="isPublic" className="text-sm cursor-pointer">
                Visible to all authenticated staff
              </label>
            </div>
          </div>

          <div className="flex gap-2">
            <button type="submit"
              disabled={createMut.isPending || updateMut.isPending}
              className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />
              {(createMut.isPending || updateMut.isPending) ? t("saving") : t("save")}
            </button>
            <button type="button" onClick={closeForm}
              className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {/* ── Filters ── */}
      <div className="flex gap-3 flex-wrap">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input value={search} onChange={e => setSearch(e.target.value)}
            placeholder="Search documents…" className="input pl-10 w-full" />
        </div>
        <select value={catFilter} onChange={e => setCat(e.target.value)} className="input">
          <option value="">All Categories</option>
          {CATEGORIES.map(c => <option key={c}>{c}</option>)}
        </select>
      </div>

      {/* ── Document cards ── */}
      {isLoading && (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filtered.map((doc: any) => (
          <div key={doc.id}
            className="card p-4 space-y-3 group hover:border-primary/30 transition-colors">

            {/* Card header */}
            <div className="flex items-start justify-between gap-2">
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-sm truncate" title={doc.name}>{doc.name}</p>
                {doc.documentNumber && (
                  <p className="text-xs text-muted-foreground font-mono">{doc.documentNumber}</p>
                )}
              </div>
              <span className={`text-xs px-2 py-0.5 rounded-full font-medium shrink-0 ${
                CAT_COLORS[doc.category] ?? "bg-muted text-muted-foreground"
              }`}>
                {doc.category}
              </span>
            </div>

            {/* Description */}
            {doc.description && (
              <p className="text-xs text-muted-foreground line-clamp-2">{doc.description}</p>
            )}

            {/* Metadata */}
            <div className="flex items-center justify-between text-xs text-muted-foreground">
              <span>{new Date(doc.createdAt).toLocaleDateString()}</span>
              {doc.expiryDate && (
                <span className={doc.isExpired ? "text-red-600 font-medium" : ""}>
                  {doc.isExpired ? "⚠ Expired" : `Expires ${doc.expiryDate}`}
                </span>
              )}
            </div>
            {doc.uploadedByName && (
              <p className="text-xs text-muted-foreground">Uploaded by {doc.uploadedByName}</p>
            )}

            {/* ── Action buttons ── */}
            <div className="flex items-center justify-between pt-1 border-t border-border/40">

              {/* Download / Open — visible to ALL authenticated users */}
              <div className="flex items-center gap-2">
                {doc.storagePath ? (
                  <>
                    <button
                      onClick={() => handleDownload(doc)}
                      className="flex items-center gap-1 text-xs px-3 py-1.5 rounded-lg bg-primary text-primary-foreground hover:bg-primary/90 font-medium transition-colors"
                      title="Download or open document"
                    >
                      <Download className="h-3.5 w-3.5" />
                      Download
                    </button>
                    <a
                      href={doc.storagePath}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-1 text-xs px-2.5 py-1.5 rounded-lg border hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
                      title="Open in new tab"
                    >
                      <ExternalLink className="h-3.5 w-3.5" />
                      Open
                    </a>
                  </>
                ) : (
                  <span className="text-xs text-muted-foreground italic">No file attached</span>
                )}
              </div>

              {/* Edit / Delete — only for permitted roles */}
              <div className="flex items-center gap-1">
                {canEdit && (
                  <button onClick={() => openEdit(doc)}
                    title={t("edit")}
                    className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground transition-colors">
                    <Pencil className="h-3.5 w-3.5" />
                  </button>
                )}
                {canDelete && (
                  <button onClick={() => setDelTarget(doc)}
                    title={t("delete")}
                    className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors">
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                )}
              </div>
            </div>
          </div>
        ))}

        {filtered.length === 0 && !isLoading && (
          <div className="col-span-3 card p-8 text-center text-muted-foreground">
            {docs?.length === 0
              ? canUpload
                ? "No documents yet. Click \"Add Document\" to upload one."
                : "No documents available yet."
              : `No documents match your search.`}
          </div>
        )}
      </div>
    </div>
  );
}
