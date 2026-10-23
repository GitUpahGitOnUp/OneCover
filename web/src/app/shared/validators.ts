// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     custom form validators shared across pages
// Layer:    shared (pure functions)
// Concepts: a ValidatorFn returns null (valid) or an errors object ({ endBeforeStart: true }).
//           CROSS-FIELD validators (endAfterStart, withinOneYear) run on the whole FormGroup b/c they compare 2 fields.
//           a validator FACTORY (startOnOrAfter) takes settings and RETURNS a ValidatorFn; here the setting is a
//           function, so the minimum can change later (it depends on which coverage was picked in step 1).
//           values may be "yyyy-MM-dd" strings OR Date objects (from a datepicker): toIsoDate turns both into the
//           same sortable string, so a plain < comparison works.
//           same rules as BenefitClaim.Submit on the API: the browser catches them early, the API still enforces them
// Related:  shared/dates.ts, features/member/file-claim.ts, BenefitClaim.Submit (the server-side twin), validators.spec.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { toIsoDate } from './dates';

/** Group validator: the endDate control must not be before the startDate control (same day is fine). */
export const endAfterStart: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const start = toIsoDate(group.get('startDate')?.value);
  const end = toIsoDate(group.get('endDate')?.value);
  return start && end && end < start ? { endBeforeStart: true } : null;
};

/** Group validator: endDate can be at most one year after startDate (the API's cap on a single claim). */
export const withinOneYear: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const start = toIsoDate(group.get('startDate')?.value);
  const end = toIsoDate(group.get('endDate')?.value);
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
    const value = toIsoDate(control.value);
    const min = minDate();
    return value && min && value < min ? { beforeCoverage: { min } } : null;
  };
}
