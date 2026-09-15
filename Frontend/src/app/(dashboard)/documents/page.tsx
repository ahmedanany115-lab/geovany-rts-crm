"use client";

import { useState, useRef } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useToast } from "@/components/ui/toast";
import { useRoles } from "@/hooks/useRoles";
import {
  FileText, Plus, X, Trash2, RefreshCw,
  Search, AlertTriangle, Download, ExternalLink,
  Upload, File,
} from "lucide-react";

/* ── Permission flags ─────────────────────────────────────────────────────── */
// Upload/Edit: Admin, Manager, Accountant, Marketing  (matches backend roles)
// Delete:      Admin, Manager only
// Download:    ALL authenticated users

export default function DocumentsPage() {
  const { toast } = useToast();
  const qc = useQueryClient();
  const { isAdmin, hasRole } = useRoles();

  const canUpload = isAdmin || hasRole("Manager", "Accountant", "Marketing");
  const canDelete = isAdmin || hasRole("Manager");

  const [search,      setSearch]     = useState("");
  const [catFilter,   setCat]        = useState("");
  const [showModal,   setShowModal]  = useState(false);
  const [delTarget,   setDelTarget]  = useState<any>(null);

  // Upload form state (minimal: name + PDF)
  const [docName,  setDocName]  = useState("");
  const [pdfFile,  setPdfFile]  = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  /* ── Data ── */
  const { data: docs, isLoading, refetch } = useQuery({
    queryKey: ["documents", catFilter],
    queryFn: () => apiFetch<any[]>(
      `/documents${catFilter ? `?category=${encodeURIComponent(catFilter)}` : ""}`
    ),
  });

  /* ── Delete ── */
  const delMut = useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/documents/${id}`, { method: "DELETE" }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["documents"] });
      toast("Deleted.", "info");
      setDelTarget(null);
    },
    onError: (e: any) => toast(e?.message ?? "Failed to delete.", "error"),
  });

  /* ── Upload ─────────────────────────────────────────────────────────────── */
  // We store the PDF as a base64 data URL in storagePath (no cloud yet).
  // The backend /documents endpoint stores the metadata; the file content
  // is embedded in StoragePath for now so authenticated users can open it.
  const handleUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!docName.trim()) { toast("Please enter a document name.", "error"); return; }
    if (!pdfFile)         { toast("Please choose a PDF file.", "error"); return; }
    if (pdfFile.type !== "application/pdf") { toast("Only PDF files are allowed.", "error"); return; }
    if (pdfFile.size > 20 * 1024 * 1024)    { toast("File is too large (max 20 MB).", "error"); return; }

    setUploading(true);
    try {
      // Convert to base64 data URL so it can be downloaded without a file server
      const dataUrl = await new Promise<string>((res, rej) => {
        const r = new FileReader();
        r.onload = () => res(r.result as string);
        r.onerror = () => rej(new Error("File read failed"));
        r.readAsDataURL(pdfFile);
      });

      await apiFetch("/documents", {
        method: "POST",
        body: JSON.stringify({
          name:          docName.trim(),
          category:      "Company Registration",   // default; not shown in UI
          description:   null,
          documentNumber: null,
          fileName:      pdfFile.name,
          originalName:  pdfFile.name,
          contentType:   "application/pdf",
          fileSizeBytes: pdfFile.size,
          storagePath:   dataUrl,            // base64 data URL — authenticated download
          uploadedByName: "",                // server fills this from JWT
          expiryDate:    null,
          isPublic:      true,
        }),
      });

      toast(`"${docName.trim()}" uploaded successfully.`, "success");
      qc.invalidateQueries({ queryKey: ["documents"] });
      setShowModal(false);
      setDocName("");
      setPdfFile(null);
      if (fileRef.current) fileRef.current.value = "";
    } catch (err: any) {
      toast(err?.message ?? "Upload failed. Please try again.", "error");
    } finally {
      setUploading(false);
    }
  };

  /* ── Download ─────────────────────────────────────────────────────────────
   * For data-URL documents: trigger browser download directly (auth is
   * guaranteed because this is a client-side page behind [Authorize]).
   * For external https:// links: open in new tab via the auth-gated endpoint.
   */
  const handleDownload = (doc: any) => {
    const path: string = doc.storagePath ?? "";
    if (!path) { toast("No file attached to this document.", "error"); return; }

    if (path.startsWith("data:")) {
      // Inline base64 PDF — trigger browser download
      const a = document.createElement("a");
      a.href     = path;
      a.download = doc.originalName ?? `${doc.name}.pdf`;
      a.click();
    } else {
      // External URL or server path — go through auth-gated backend
      const apiBase = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(/\/api\/v1\/?$/, "");
      window.open(`${apiBase}/api/v1/documents/${doc.id}/download`, "_blank", "noopener,noreferrer");
    }
  };

  /* ── Filtered list ── */
  const filtered = (docs ?? []).filter(d =>
    !search
      || d.name.toLowerCase().includes(search.toLowerCase())
      || (d.uploadedByName ?? "").toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="p-6 space-y-6">

      {/* ── Delete confirm ── */}
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
              <button onClick={() => setDelTarget(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={() => delMut.mutate(delTarget.id)} disabled={delMut.isPending}
                className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">
                {delMut.isPending ? "Deleting…" : "Delete"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Upload modal ── */}
      {showModal && canUpload && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 w-full max-w-md space-y-5">
            <div className="flex items-center justify-between">
              <h2 className="font-semibold text-lg">Add Document</h2>
              <button onClick={() => { setShowModal(false); setDocName(""); setPdfFile(null); }}
                className="p-1.5 rounded hover:bg-accent text-muted-foreground">
                <X className="h-4 w-4" />
              </button>
            </div>

            <form onSubmit={handleUpload} className="space-y-4">
              {/* Document name */}
              <div>
                <label className="text-sm font-medium block mb-1.5">Document Name <span className="text-red-500">*</span></label>
                <input
                  required
                  value={docName}
                  onChange={e => setDocName(e.target.value)}
                  placeholder="e.g. Company Tax Card 2025"
                  className="input w-full"
                  autoFocus
                />
              </div>

              {/* PDF file chooser */}
              <div>
                <label className="text-sm font-medium block mb-1.5">PDF File <span className="text-red-500">*</span></label>
                <div
                  onClick={() => fileRef.current?.click()}
                  className="border-2 border-dashed rounded-xl p-6 text-center cursor-pointer hover:border-primary/60 hover:bg-muted/30 transition-colors"
                >
                  {pdfFile ? (
                    <div className="flex items-center justify-center gap-2 text-primary">
                      <File className="h-6 w-6 shrink-0" />
                      <span className="font-medium text-sm truncate max-w-xs">{pdfFile.name}</span>
                      <span className="text-xs text-muted-foreground">({(pdfFile.size / 1024).toFixed(0)} KB)</span>
                    </div>
                  ) : (
                    <div className="text-muted-foreground">
                      <Upload className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p className="text-sm font-medium">Click to choose PDF</p>
                      <p className="text-xs mt-0.5">PDF only · Max 20 MB</p>
                    </div>
                  )}
                  <input
                    ref={fileRef}
                    type="file"
                    accept="application/pdf,.pdf"
                    className="hidden"
                    onChange={e => setPdfFile(e.target.files?.[0] ?? null)}
                  />
                </div>
              </div>

              <div className="flex gap-2 pt-1">
                <button type="button"
                  onClick={() => { setShowModal(false); setDocName(""); setPdfFile(null); }}
                  className="btn-ghost flex-1 py-2 rounded-lg text-sm">
                  Cancel
                </button>
                <button type="submit" disabled={uploading || !docName.trim() || !pdfFile}
                  className="btn-primary flex-1 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center justify-center gap-2">
                  {uploading
                    ? <><span className="h-3.5 w-3.5 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Uploading…</>
                    : <><Upload className="h-3.5 w-3.5" /> Upload</>}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── Page header ── */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <FileText className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">Company Documents</h1>
            <p className="text-sm text-muted-foreground">
              {canUpload ? "Upload and manage company PDF documents" : "Browse and download company documents"}
            </p>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          {canUpload && (
            <button onClick={() => setShowModal(true)}
              className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
              <Plus className="h-4 w-4" /> Add Document
            </button>
          )}
        </div>
      </div>

      {/* ── Search ── */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <input value={search} onChange={e => setSearch(e.target.value)}
          placeholder="Search documents…" className="input pl-10 w-full" />
      </div>

      {/* ── Document list ── */}
      {isLoading && (
        <div className="text-center py-10 text-muted-foreground">Loading documents…</div>
      )}

      {!isLoading && filtered.length === 0 && (
        <div className="card p-8 text-center text-muted-foreground">
          {docs?.length === 0
            ? canUpload ? 'No documents yet. Click "+ Add Document" to upload.' : "No documents available yet."
            : `No documents match your search.`}
        </div>
      )}

      <div className="card overflow-hidden">
        {filtered.length > 0 && (
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground w-10"></th>
                <th className="text-left p-3 font-medium text-muted-foreground">Document Name</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Uploaded By</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
                <th className="p-3 text-right text-muted-foreground">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filtered.map((doc: any) => (
                <tr key={doc.id} className="hover:bg-muted/20 group">
                  {/* PDF icon */}
                  <td className="p-3">
                    <div className="h-9 w-9 rounded-lg bg-red-100 dark:bg-red-950/30 flex items-center justify-center">
                      <FileText className="h-4.5 w-4.5 text-red-600" />
                    </div>
                  </td>

                  {/* Name */}
                  <td className="p-3">
                    <p className="font-medium">{doc.name}</p>
                    {doc.originalName && doc.originalName !== doc.name && (
                      <p className="text-xs text-muted-foreground">{doc.originalName}</p>
                    )}
                    {doc.isExpired && (
                      <span className="text-xs text-red-600 font-medium">⚠ Expired</span>
                    )}
                  </td>

                  {/* Uploaded by */}
                  <td className="p-3 text-muted-foreground">
                    {doc.uploadedByName || "—"}
                  </td>

                  {/* Date */}
                  <td className="p-3 text-muted-foreground">
                    {new Date(doc.createdAt).toLocaleDateString()}
                  </td>

                  {/* Actions */}
                  <td className="p-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      {/* Download — ALL authenticated users */}
                      {doc.storagePath ? (
                        <button
                          onClick={() => handleDownload(doc)}
                          className="flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-lg bg-primary text-primary-foreground hover:bg-primary/90 font-medium transition-colors"
                          title="Download PDF"
                        >
                          <Download className="h-3.5 w-3.5" /> Download
                        </button>
                      ) : (
                        <span className="text-xs text-muted-foreground italic">No file</span>
                      )}

                      {/* Delete — Admin/Manager only */}
                      {canDelete && (
                        <button onClick={() => setDelTarget(doc)}
                          className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors opacity-0 group-hover:opacity-100"
                          title="Delete">
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {filtered.length > 0 && (
        <p className="text-xs text-muted-foreground text-center">
          {filtered.length} document{filtered.length !== 1 ? "s" : ""}
          {!canUpload && " — You can view and download all company documents."}
        </p>
      )}
    </div>
  );
}
