"use client";

import { useCurrentUser } from "@/features/auth/hooks/useAuth";

export default function ProfilePage() {
  const user = useCurrentUser();

  if (!user) return null;

  return (
    <div className="max-w-lg space-y-6">
      <h1 className="text-2xl font-semibold">Profile</h1>

      <div className="flex items-center gap-4">
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-accent text-lg font-medium">
          {user.firstName[0]}
          {user.lastName[0]}
        </div>
        <div>
          <p className="font-medium">
            {user.firstName} {user.lastName}
          </p>
          <p className="text-sm text-muted-foreground">{user.email}</p>
        </div>
      </div>

      <div>
        <h2 className="mb-2 text-sm font-medium">Roles</h2>
        <div className="flex flex-wrap gap-2">
          {user.roles.map((role) => (
            <span key={role} className="rounded-full border px-3 py-1 text-xs">
              {role}
            </span>
          ))}
        </div>
      </div>

      {/* Password change / notification preferences land once Settings/Users modules exist */}
    </div>
  );
}
