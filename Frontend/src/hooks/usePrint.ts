/**
 * Reusable print hook.
 * Usage: const { printRef, handlePrint } = usePrint();
 * Wrap printable content in <div ref={printRef}> and call handlePrint() on button click.
 */
import { useRef, useCallback } from "react";

export function usePrint(title?: string) {
  const printRef = useRef<HTMLDivElement>(null);

  const handlePrint = useCallback(() => {
    if (!printRef.current) return;
    const originalTitle = document.title;
    if (title) document.title = title;
    window.print();
    if (title) document.title = originalTitle;
  }, [title]);

  return { printRef, handlePrint };
}
