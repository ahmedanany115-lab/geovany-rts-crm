"use client";
import { useState, useEffect } from "react";
import { useToast } from "@/components/ui/toast";
import { useT } from "@/hooks/useT";
import { useAuthStore } from "@/stores/auth-store";
import { Briefcase, Plus, X, Trash2, Clock, CheckCircle, Circle, PauseCircle } from "lucide-react";

interface Project {
  id: string;
  title: string;
  description: string;
  status: "active" | "on-hold" | "completed";
  owner: string;
  dueDate: string;
  createdAt: string;
}

const STATUS_CONFIG = {
  active:    { label: "Active",    icon: Circle,       cls: "bg-blue-100 text-blue-700" },
  "on-hold": { label: "On Hold",   icon: PauseCircle,  cls: "bg-amber-100 text-amber-700" },
  completed: { label: "Completed", icon: CheckCircle,  cls: "bg-emerald-100 text-emerald-700" },
};

const STORAGE_KEY = "rts_projects";

export default function ProjectsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const user = useAuthStore(s => s.user);
  const [projects, setProjects] = useState<Project[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ title: "", description: "", status: "active" as Project["status"], dueDate: "" });

  useEffect(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) setProjects(JSON.parse(stored));
    } catch {}
  }, []);

  const save = (updated: Project[]) => {
    setProjects(updated);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(updated)); } catch {}
  };

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.title.trim()) return;
    const project: Project = {
      id: crypto.randomUUID(),
      title: form.title.trim(),
      description: form.description.trim(),
      status: form.status,
      owner: `${user?.firstName ?? ""} ${user?.lastName ?? ""}`.trim() || user?.email || "Me",
      dueDate: form.dueDate,
      createdAt: new Date().toISOString(),
    };
    save([project, ...projects]);
    toast(`Project "${project.title}" created.`, "success");
    setForm({ title: "", description: "", status: "active", dueDate: "" });
    setShowForm(false);
  };

  const handleDelete = (id: string, title: string) => {
    save(projects.filter(p => p.id !== id));
    toast(`"${title}" deleted.`, "info");
  };

  const cycleStatus = (id: string) => {
    const order: Project["status"][] = ["active", "on-hold", "completed"];
    save(projects.map(p => p.id === id ? { ...p, status: order[(order.indexOf(p.status) + 1) % 3] } : p));
  };

  const counts = { active: 0, "on-hold": 0, completed: 0 };
  projects.forEach(p => counts[p.status]++);

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Briefcase className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">{t("projects")}</h1>
        </div>
        <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
          {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
          {showForm ? t("cancel") : `${t("add")} Project`}
        </button>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-3 gap-4">
        {(Object.entries(STATUS_CONFIG) as [Project["status"], typeof STATUS_CONFIG[keyof typeof STATUS_CONFIG]][]).map(([key, cfg]) => (
          <div key={key} className="card p-4 text-center">
            <cfg.icon className={`h-5 w-5 mx-auto mb-1 ${cfg.cls.split(" ")[1]}`} />
            <p className="text-2xl font-bold tabular-nums">{counts[key]}</p>
            <p className="text-xs text-muted-foreground">{cfg.label}</p>
          </div>
        ))}
      </div>

      {/* Form */}
      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-3 border border-primary/20">
          <h2 className="font-semibold text-sm">New Project</h2>
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Title *</label><input required value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} className="input w-full" /></div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Description</label><textarea rows={2} value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} className="input w-full resize-none" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Status</label>
              <select value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value as Project["status"] }))} className="input w-full">
                <option value="active">Active</option><option value="on-hold">On Hold</option><option value="completed">Completed</option>
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Due Date</label><input type="date" value={form.dueDate} onChange={e => setForm(f => ({ ...f, dueDate: e.target.value }))} className="input w-full" /></div>
          </div>
          <button type="submit" className="btn-primary px-5 py-2 rounded-lg text-sm">{t("save")}</button>
        </form>
      )}

      {/* Projects list */}
      <div className="space-y-3">
        {projects.length === 0 ? (
          <div className="card p-12 text-center text-muted-foreground">No projects yet. Click "Add Project" to create one.</div>
        ) : (
          projects.map(p => {
            const cfg = STATUS_CONFIG[p.status];
            const Icon = cfg.icon;
            return (
              <div key={p.id} className="card p-4 flex items-start gap-4">
                <button onClick={() => cycleStatus(p.id)} title="Click to cycle status" className="mt-0.5">
                  <Icon className={`h-5 w-5 ${cfg.cls.split(" ")[1]}`} />
                </button>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <h3 className="font-semibold">{p.title}</h3>
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cfg.cls}`}>{cfg.label}</span>
                  </div>
                  {p.description && <p className="text-sm text-muted-foreground mt-0.5 truncate">{p.description}</p>}
                  <div className="flex items-center gap-3 mt-1 text-xs text-muted-foreground">
                    <span>Owner: {p.owner}</span>
                    {p.dueDate && <span className="flex items-center gap-1"><Clock className="h-3 w-3" />Due: {p.dueDate}</span>}
                  </div>
                </div>
                <button onClick={() => handleDelete(p.id, p.title)} className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
