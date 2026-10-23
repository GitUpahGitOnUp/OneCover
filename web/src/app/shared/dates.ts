// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     converts between the API's date-only strings ("2026-10-23") and the Date objects a Material datepicker uses
// Layer:    shared (pure functions)
// Concepts: uses the LOCAL calendar day on purpose. toISOString() would convert to UTC first, so a date picked late
//           in the evening (US time) could come out as TOMORROW. new Date("2026-10-23") has the opposite problem
//           (it's parsed as UTC midnight = the evening before in the US), so fromIsoDate builds the Date from its parts
// Related:  shared/validators.ts, features/member/file-claim.ts, the API's DateOnly fields
// ─────────────────────────────────────────────────────────────────────────────────────────

/** A date value as "yyyy-MM-dd": strings pass through; Date objects use their LOCAL day. Null if empty/invalid. */
export function toIsoDate(value: unknown): string | null {
  if (value instanceof Date) {
    if (isNaN(value.getTime())) {
      return null;   // e.g. a half-typed date the datepicker couldn't parse
    }
    const month = String(value.getMonth() + 1).padStart(2, '0');   // getMonth() is 0-based
    const day = String(value.getDate()).padStart(2, '0');
    return `${value.getFullYear()}-${month}-${day}`;
  }
  return typeof value === 'string' && value ? value : null;
}

/** "2026-10-23" -> a Date at LOCAL midnight on that day (for a datepicker's [min]). */
export function fromIsoDate(iso: string): Date {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day);
}
