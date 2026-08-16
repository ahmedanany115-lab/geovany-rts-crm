import { LoginForm } from "@/features/auth/components/LoginForm";

export default function LoginPage() {
  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <h1 className="text-2xl font-semibold">Royal CRM</h1>
          <p className="mt-1 text-sm text-muted-foreground">Sign in to your Royal CRM account</p>
        </div>
        <LoginForm />
      </div>
    </div>
  );
}
