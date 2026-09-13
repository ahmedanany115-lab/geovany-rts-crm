"use client";
import { useState, useEffect } from "react";
import { useToast } from "@/components/ui/toast";
import { useT } from "@/hooks/useT";
import { useAuthStore } from "@/stores/auth-store";
import { KanbanSquare, Plus, X, Trash2 } from "lucide-react";

type Column = "todo" | "in-progress" | "done";
interface Task { id: string; title: string; notes: string; column: Column; assignee: string; createdAt: string; }

const COLUMNS: { key: Column; label: string; cls: string }[] = [
  { key: "todo",        label: "To Do",       cls: "border-t-slate-400" },
  { key: "in-progress", label: "In Progress", cls: "border-t-blue-500" },
  { key: "done",        label: "Done",        cls: "border-t-emerald-500" },
];

const STORAGE_KEY = "rts_tasks";

export default function TasksBoardPage() {
  const { toast } = useToast();
  const user = useAuthStore(s => s.user);
  const [tasks, setTasks] = useState<Task[]>([]);
  const [adding, setAdding] = useState<Column | null>(null);
  const [form, setForm] = useState({ title: "", notes: "" });

  useEffect(() => {
    try { const s = localStorage.getItem(STORAGE_KEY); if (s) setTasks(JSON.parse(s)); } catch {}
  }, []);

  const save = (updated: Task[]) => {
    setTasks(updated);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(updated)); } catch {}
  };

  const addTask = (column: Column) => {
    if (!form.title.trim()) return;
    const task: Task = {
      id: crypto.randomUUID(), title: form.title.trim(), notes: form.notes.trim(),
      column, assignee: `${user?.firstName ?? ""} ${user?.lastName ?? ""}`.trim() || "Me",
      createdAt: new Date().toISOString(),
    };
    save([task, ...tasks]);
    toast(`Task "${task.title}" added.`, "success");
    setForm({ title: "", notes: "" }); setAdding(null);
  };

  const moveTask = (id: string, to: Column) =>
    save(tasks.map(t => t.id === id ? { ...t, column: to } : t));

  const deleteTask = (id: string, title: string) => {
    save(tasks.filter(t => t.id !== id));
    toast(`"${title}" removed.`, "info");
  };

  const colTasks = (col: Column) => tasks.filter(t => t.column === col);

  return (
    <div className="p-6 space-y-6 h-full flex flex-col">
      <div className="flex items-center gap-3">
        <KanbanSquare className="h-6 w-6 text-primary" />
        <h1 className="text-2xl font-semibold">Tasks — Kanban Board</h1>
        <span className="text-xs text-muted-foreground ml-2">{tasks.length} task{tasks.length !== 1 ? "s" : ""}</span>
      </div>

      <div className="grid grid-cols-3 gap-4 flex-1 min-h-0">
        {COLUMNS.map(col => (
          <div key={col.key} className={`flex flex-col rounded-lg border border-t-4 ${col.cls} bg-muted/20 overflow-hidden`}>
            {/* Column header */}
            <div className="flex items-center justify-between px-3 py-2 border-b bg-background">
              <h2 className="font-semibold text-sm">{col.label}</h2>
              <div className="flex items-center gap-1">
                <span className="text-xs text-muted-foreground bg-muted px-1.5 py-0.5 rounded-full">{colTasks(col.key).length}</span>
                <button onClick={() => { setAdding(col.key); setForm({ title: "", notes: "" }); }}
                  className="p-0.5 hover:bg-accent rounded">
                  <Plus className="h-4 w-4 text-muted-foreground" />
                </button>
              </div>
            </div>

            {/* Add form */}
            {adding === col.key && (
              <div className="p-2 border-b bg-background space-y-2">
                <input autoFocus value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                  onKeyDown={e => { if (e.key === "Enter") { e.preventDefault(); addTask(col.key); } if (e.key === "Escape") setAdding(null); }}
                  placeholder="Task title..." className="input w-full text-sm py-1.5" />
                <textarea rows={2} value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
                  placeholder="Notes (optional)..." className="input w-full text-sm py-1.5 resize-none" />
                <div className="flex gap-1">
                  <button onClick={() => addTask(col.key)} className="flex-1 text-xs py-1.5 rounded bg-primary text-primary-foreground hover:bg-primary/90">Add</button>
                  <button onClick={() => setAdding(null)} className="p-1.5 rounded hover:bg-accent"><X className="h-3.5 w-3.5" /></button>
                </div>
              </div>
            )}

            {/* Task cards */}
            <div className="flex-1 overflow-y-auto p-2 space-y-2">
              {colTasks(col.key).map(task => (
                <div key={task.id} className="bg-background rounded-md border p-3 shadow-sm group">
                  <div className="flex items-start justify-between gap-2">
                    <p className="text-sm font-medium leading-snug">{task.title}</p>
                    <button onClick={() => deleteTask(task.id, task.title)}
                      className="opacity-0 group-hover:opacity-100 p-0.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 shrink-0">
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                  {task.notes && <p className="text-xs text-muted-foreground mt-1 line-clamp-2">{task.notes}</p>}
                  <p className="text-xs text-muted-foreground mt-2">{task.assignee}</p>
                  {/* Move buttons */}
                  <div className="flex gap-1 mt-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    {COLUMNS.filter(c => c.key !== col.key).map(c => (
                      <button key={c.key} onClick={() => moveTask(task.id, c.key)}
                        className="flex-1 text-xs py-0.5 rounded bg-muted hover:bg-accent text-muted-foreground hover:text-foreground">
                        → {c.label}
                      </button>
                    ))}
                  </div>
                </div>
              ))}
              {colTasks(col.key).length === 0 && adding !== col.key && (
                <div className="text-center py-8 text-xs text-muted-foreground/50">
                  Drop tasks here
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
