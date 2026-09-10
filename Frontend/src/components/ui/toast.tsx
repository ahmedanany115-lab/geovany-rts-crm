"use client";
// Lightweight toast built on @radix-ui/react-toast (already installed)
import * as Toast from "@radix-ui/react-toast";
import { createContext, useContext, useState, useCallback } from "react";
import { X, CheckCircle, AlertCircle, Info } from "lucide-react";

type Variant = "success" | "error" | "info";

interface ToastItem {
  id: number;
  message: string;
  variant: Variant;
}

interface ToastContextValue {
  toast: (message: string, variant?: Variant) => void;
}

const ToastContext = createContext<ToastContextValue>({ toast: () => {} });

export function useToast() {
  return useContext(ToastContext);
}

const ICONS = {
  success: <CheckCircle className="h-4 w-4 text-emerald-600 shrink-0" />,
  error:   <AlertCircle  className="h-4 w-4 text-red-600 shrink-0" />,
  info:    <Info          className="h-4 w-4 text-blue-600 shrink-0" />,
};

const BG = {
  success: "border-emerald-200 bg-emerald-50 dark:bg-emerald-950/40",
  error:   "border-red-200 bg-red-50 dark:bg-red-950/40",
  info:    "border-blue-200 bg-blue-50 dark:bg-blue-950/40",
};

let _id = 0;

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([]);

  const toast = useCallback((message: string, variant: Variant = "info") => {
    const id = ++_id;
    setItems(prev => [...prev, { id, message, variant }]);
    setTimeout(() => setItems(prev => prev.filter(i => i.id !== id)), 4000);
  }, []);

  return (
    <ToastContext.Provider value={{ toast }}>
      <Toast.Provider swipeDirection="right">
        {children}
        {items.map(item => (
          <Toast.Root
            key={item.id}
            open
            onOpenChange={() => setItems(prev => prev.filter(i => i.id !== item.id))}
            className={`flex items-start gap-3 rounded-lg border px-4 py-3 shadow-lg text-sm max-w-sm
              data-[state=open]:animate-in data-[state=open]:slide-in-from-right-full
              data-[state=closed]:animate-out data-[state=closed]:slide-out-to-right-full
              ${BG[item.variant]}`}
          >
            {ICONS[item.variant]}
            <Toast.Description className="flex-1 text-foreground">{item.message}</Toast.Description>
            <Toast.Close className="text-muted-foreground hover:text-foreground">
              <X className="h-3.5 w-3.5" />
            </Toast.Close>
          </Toast.Root>
        ))}
        <Toast.Viewport className="fixed bottom-4 right-4 z-50 flex flex-col gap-2 outline-none" />
      </Toast.Provider>
    </ToastContext.Provider>
  );
}
