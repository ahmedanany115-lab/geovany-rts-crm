// Convenience hook — returns a t() bound to the current language
"use client";
import { useUiStore } from "@/stores/ui-state";
import { t as tFn, type TKey } from "@/lib/i18n";

export function useT() {
  const lang = useUiStore((s) => s.language);
  return { lang, t: (key: TKey) => tFn(key, lang) };
}
