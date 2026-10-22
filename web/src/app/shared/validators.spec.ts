// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for the claim form's date validators
// Layer:    Tests (Vitest)
// Concepts: validators are PURE FUNCTIONS (control in -> errors or null out), so they're tested by building a tiny
//           FormGroup/FormControl and calling them directly. EDGE CASES: same day, exactly one year, no minimum yet
// Related:  shared/validators.ts, features/member/file-claim.ts, the API's BenefitClaim.Submit (same rules)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { FormControl, FormGroup } from '@angular/forms';
import { endAfterStart, startOnOrAfter, withinOneYear } from './validators';

// a minimal stand-in for the claim form's dates group
function dates(startDate: string, endDate: string) {
  return new FormGroup({ startDate: new FormControl(startDate), endDate: new FormControl(endDate) });
}

describe('endAfterStart', () => {
  it('passes when the end is after the start', () => {
    expect(endAfterStart(dates('2026-10-01', '2026-10-14'))).toBeNull();
  });

  it('passes when both are the same day', () => {
    expect(endAfterStart(dates('2026-10-01', '2026-10-01'))).toBeNull();
  });

  it('fails when the end is before the start', () => {
    expect(endAfterStart(dates('2026-10-14', '2026-10-01'))).toEqual({ endBeforeStart: true });
  });

  it('ignores a missing date (the required validator reports that)', () => {
    expect(endAfterStart(dates('', '2026-10-01'))).toBeNull();
  });
});

describe('withinOneYear', () => {
  it('allows exactly one year', () => {
    expect(withinOneYear(dates('2026-03-15', '2027-03-15'))).toBeNull();
  });

  it('rejects one day more than a year', () => {
    expect(withinOneYear(dates('2026-03-15', '2027-03-16'))).toEqual({ tooLong: true });
  });
});

describe('startOnOrAfter', () => {
  const afterJan1 = startOnOrAfter(() => '2026-01-01');

  it('rejects a date before the minimum', () => {
    expect(afterJan1(new FormControl('2025-12-31'))).toEqual({ beforeCoverage: { min: '2026-01-01' } });
  });

  it('allows the minimum itself', () => {
    expect(afterJan1(new FormControl('2026-01-01'))).toBeNull();
  });

  it('allows anything while there is no minimum yet (no coverage picked)', () => {
    expect(startOnOrAfter(() => undefined)(new FormControl('2000-01-01'))).toBeNull();
  });
});
