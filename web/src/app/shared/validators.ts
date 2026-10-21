// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     custom form validators shared across pages
// Layer:    shared (pure functions)
// Concepts: a CROSS-FIELD validator: it runs on the whole FormGroup, not one control, b/c "end after start"
//           compares two fields. a ValidatorFn returns null (valid) or an errors object ({ endBeforeStart: true }).
//           "yyyy-MM-dd" strings sort the same way as the dates they represent, so a plain < comparison works.
//           same rule as BenefitClaim.Submit on the API: the browser catches it early, the API still enforces it
// Related:  features/member/file-claim.ts (uses it), BenefitClaim.Submit (the server-side twin), Day 9 Vitest test
// ─────────────────────────────────────────────────────────────────────────────────────────
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Group validator: the endDate control must not be before the startDate control (same day is fine). */
export const endAfterStart: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const start = group.get('startDate')?.value as string | null;
  const end = group.get('endDate')?.value as string | null;
  return start && end && end < start ? { endBeforeStart: true } : null;
};
