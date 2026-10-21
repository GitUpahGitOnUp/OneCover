// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     custom form validators shared across pages
// Layer:    shared (pure functions)
// Concepts: a ValidatorFn returns null (valid) or an errors object ({ endBeforeStart: true }).
//           CROSS-FIELD validators (endAfterStart, withinOneYear) run on the whole FormGroup b/c they compare 2 fields.
//           a validator FACTORY (startOnOrAfter) takes settings and RETURNS a ValidatorFn; here the setting is a
//           function, so the minimum can change later (it depends on which coverage was picked in step 1).
//           "yyyy-MM-dd" strings sort the same way as the dates they represent, so a plain < comparison works.
//           same rules as BenefitClaim.Submit on the API: the browser catches them early, the API still enforces them
// Related:  features/member/file-claim.ts (uses them), BenefitClaim.Submit (the server-side twin), Day 9 Vitest test
// ─────────────────────────────────────────────────────────────────────────────────────────
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Group validator: the endDate control must not be before the startDate control (same day is fine). */
export const endAfterStart: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const start = group.get('startDate')?.value as string | null;
  const end = group.get('endDate')?.value as string | null;
  return start && end && end < start ? { endBeforeStart: true } : null;
};

/** Group validator: endDate can be at most one year after startDate (the API's cap on a single claim). */
export const withinOneYear: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const start = group.get('startDate')?.value as string | null;
  const end = group.get('endDate')?.value as string | null;
  if (!start || !end) {
    return null;   // the required validators report missing dates
  }
  // "2026-03-15" -> "2027-03-15": bump the year, keep month + day, still a sortable string
  const [year, rest] = [start.slice(0, 4), start.slice(4)];
  const oneYearLater = `${String(Number(year) + 1).padStart(4, '0')}${rest}`;
  return end > oneYearLater ? { tooLong: true } : null;
};

/**
 * Control validator factory: the date must be on or after `minDate()` ("yyyy-MM-dd").
 * No minimum yet (nothing selected) = valid. Call updateValueAndValidity() when the minimum changes.
 */
export function startOnOrAfter(minDate: () => string | undefined): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string | null;
    const min = minDate();
    return value && min && value < min ? { beforeCoverage: { min } } : null;
  };
}
