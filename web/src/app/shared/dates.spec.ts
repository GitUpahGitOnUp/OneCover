// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for the date helpers + the validators working w/ Date objects (from the datepicker)
// Layer:    Tests (Vitest)
// Concepts: the bug these guard against is OFF-BY-ONE-DAY: a picked date must reach the API as the SAME calendar day
// Related:  shared/dates.ts, shared/validators.ts, features/member/file-claim.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { FormControl, FormGroup } from '@angular/forms';
import { fromIsoDate, toIsoDate } from './dates';
import { endAfterStart, startOnOrAfter } from './validators';

describe('date helpers', () => {
  it('formats a Date as its LOCAL calendar day, even late in the evening', () => {
    expect(toIsoDate(new Date(2026, 9, 23, 23, 30))).toBe('2026-10-23');   // month is 0-based: 9 = October
  });

  it('passes strings through and turns empty or invalid values into null', () => {
    expect(toIsoDate('2026-10-23')).toBe('2026-10-23');
    expect(toIsoDate(null)).toBeNull();
    expect(toIsoDate('')).toBeNull();
    expect(toIsoDate(new Date('not a date'))).toBeNull();
  });

  it('round-trips: "yyyy-MM-dd" -> Date -> the same "yyyy-MM-dd"', () => {
    expect(toIsoDate(fromIsoDate('2026-01-05'))).toBe('2026-01-05');
  });
});

describe('validators with datepicker Date values', () => {
  it('endAfterStart compares Date objects', () => {
    const group = new FormGroup({
      startDate: new FormControl(new Date(2026, 9, 14)),
      endDate: new FormControl(new Date(2026, 9, 1)),
    });
    expect(endAfterStart(group)).toEqual({ endBeforeStart: true });
  });

  it('startOnOrAfter compares a Date against the "yyyy-MM-dd" minimum', () => {
    const afterOct22 = startOnOrAfter(() => '2026-10-22');
    expect(afterOct22(new FormControl(new Date(2026, 9, 21)))).toEqual({ beforeCoverage: { min: '2026-10-22' } });
    expect(afterOct22(new FormControl(new Date(2026, 9, 22)))).toBeNull();
  });
});
