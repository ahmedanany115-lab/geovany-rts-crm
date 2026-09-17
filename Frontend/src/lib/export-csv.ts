/**
 * Download data as a CSV file.
 * @param rows  Array of objects to export
 * @param filename  e.g. "sales-report.csv"
 * @param columns  Optional explicit column order/headers. If omitted, uses Object.keys(rows[0]).
 */
export function exportCsv(
  rows: Record<string, unknown>[],
  filename: string,
  columns?: { key: string; header: string }[]
) {
  if (!rows.length) return;

  const cols = columns ?? Object.keys(rows[0]).map(k => ({ key: k, header: k }));
  const escape = (v: unknown) => {
    const s = v == null ? "" : String(v);
    return s.includes(",") || s.includes('"') || s.includes("\n")
      ? `"${s.replace(/"/g, '""')}"` : s;
  };

  const header = cols.map(c => escape(c.header)).join(",");
  const body   = rows.map(r => cols.map(c => escape(r[c.key])).join(",")).join("\n");
  const csv    = `${header}\n${body}`;

  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const url  = URL.createObjectURL(blob);
  const a    = document.createElement("a");
  a.href     = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}
