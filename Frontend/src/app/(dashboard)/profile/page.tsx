"use client";
import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useCurrentUser } from "@/features/auth/hooks/useAuth";
import { useToast } from "@/components/ui/toast";
import { useAuthStore } from "@/stores/auth-store";
import { User, KeyRound, Check, AlertTriangle } from "lucide-react";

const ROLE_COLORS: Record<string, string> = {
  Admin: "bg-red-100 text-red-700", Manager: "bg-purple-100 text-purple-700",
  SalesManager: "bg-indigo-100 text-indigo-700", Accountant: "bg-blue-100 text-blue-700",
  Sales: "bg-emerald-100 text-emerald-700", Purchasing: "bg-amber-100 text-amber-700",
  SupportAgent: "bg-cyan-100 text-cyan-700", Delivery: "bg-orange-100 text-orange-700",
  Marketing: "bg-pink-100 text-pink-700", ReadOnly: "bg-gray-100 text-gray-600",
};

export default function ProfilePage() {
  const user  = useCurrentUser();
  const { toast } = useToast();
  const qc = useQueryClient();

  const [firstName, setFirstName] = useState(user?.firstName ?? "");
  const [lastName,  setLastName]  = useState(user?.lastName  ?? "");

  const [curPwd,  setCurPwd]  = useState("");
  const [newPwd,  setNewPwd]  = useState("");
  const [confPwd, setConfPwd] = useState("");
  const [pwdErr,  setPwdErr]  = useState<string | null>(null);

  const profileMut = useMutation({
    mutationFn: (data: { firstName: string; lastName: string }) =>
      apiFetch("/auth/update-profile", { method: "PATCH", body: JSON.stringify(data) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["current-user"] });
      toast("Profile updated.", "success");
    },
    onError: (e: any) => toast(e?.message ?? "Update failed.", "error"),
  });

  const pwdMut = useMutation({
    mutationFn: (data: { currentPassword: string; newPassword: string }) =>
      apiFetch("/auth/change-password", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => {
      toast("Password changed successfully.", "success");
      setCurPwd(""); setNewPwd(""); setConfPwd(""); setPwdErr(null);
    },
    onError: (e: any) => {
      const msg = e?.message ?? "Failed to change password.";
      setPwdErr(msg); toast(msg, "error");
    },
  });

  const handleProfile = (e: React.FormEvent) => {
    e.preventDefault();
    if (!firstName.trim()) { toast("First name is required.", "error"); return; }
    profileMut.mutate({ firstName: firstName.trim(), lastName: lastName.trim() });
  };

  const handlePassword = (e: React.FormEvent) => {
    e.preventDefault(); setPwdErr(null);
    if (!curPwd) { setPwdErr("Enter your current password."); return; }
    if (newPwd.length < 6) { setPwdErr("New password must be at least 6 characters."); return; }
    if (newPwd !== confPwd) { setPwdErr("Passwords do not match."); return; }
    pwdMut.mutate({ currentPassword: curPwd, newPassword: newPwd });
  };

  if (!user) return null;

  const initials = `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toUpperCase() || "U";

  return (
    <div className="p-6 max-w-xl space-y-8">
      <h1 className="text-2xl font-semibold">My Profile</h1>

      {/* Avatar + identity */}
      <div className="card p-6 space-y-5">
        <div className="flex items-center gap-4">
          <div className="h-16 w-16 rounded-full bg-primary/10 flex items-center justify-center text-2xl font-bold text-primary">
            {initials}
          </div>
          <div>
            <p className="font-semibold text-lg">{user.firstName} {user.lastName}</p>
            <p className="text-sm text-muted-foreground">{user.email}</p>
            <div className="flex flex-wrap gap-1.5 mt-1.5">
              {user.roles.map(r => (
                <span key={r} className={`text-xs px-2 py-0.5 rounded-full font-medium ${ROLE_COLORS[r] ?? "bg-muted text-muted-foreground"}`}>
                  {r}
                </span>
              ))}
            </div>
          </div>
        </div>

        {/* Edit name */}
        <form onSubmit={handleProfile} className="space-y-3 border-t pt-4">
          <p className="text-sm font-semibold flex items-center gap-2"><User className="h-4 w-4" />Edit Name</p>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">First Name *</label>
              <input value={firstName} onChange={e => setFirstName(e.target.value)} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Last Name</label>
              <input value={lastName} onChange={e => setLastName(e.target.value)} className="input w-full" /></div>
          </div>
          <button type="submit" disabled={profileMut.isPending}
            className="btn-primary px-4 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
            <Check className="h-4 w-4" />{profileMut.isPending ? "Saving…" : "Save Name"}
          </button>
        </form>
      </div>

      {/* Change password */}
      <div className="card p-6 space-y-4">
        <p className="text-sm font-semibold flex items-center gap-2"><KeyRound className="h-4 w-4" />Change Password</p>
        {pwdErr && (
          <div className="flex items-center gap-2 bg-red-50 border border-red-200 rounded-md px-4 py-2 text-sm text-red-700">
            <AlertTriangle className="h-4 w-4 shrink-0" />{pwdErr}
          </div>
        )}
        <form onSubmit={handlePassword} className="space-y-3">
          <div><label className="text-xs text-muted-foreground block mb-1">Current Password *</label>
            <input type="password" value={curPwd} onChange={e => setCurPwd(e.target.value)} className="input w-full" autoComplete="current-password" /></div>
          <div><label className="text-xs text-muted-foreground block mb-1">New Password *</label>
            <input type="password" value={newPwd} onChange={e => setNewPwd(e.target.value)} className="input w-full" autoComplete="new-password" /></div>
          <div><label className="text-xs text-muted-foreground block mb-1">Confirm New Password *</label>
            <input type="password" value={confPwd} onChange={e => setConfPwd(e.target.value)} className="input w-full" autoComplete="new-password" /></div>
          <button type="submit" disabled={pwdMut.isPending}
            className="btn-primary px-4 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
            <KeyRound className="h-4 w-4" />{pwdMut.isPending ? "Changing…" : "Change Password"}
          </button>
        </form>
      </div>
    </div>
  );
}
