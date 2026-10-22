// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for MaskSsnPipe
// Layer:    Tests (Vitest)
// Concepts: describe = a group of tests (like an xUnit test class), it = one test (like [Fact]),
//           expect(...).toBe(...) = the assertion (like Assert.Equal). a pipe is a plain class, so no TestBed needed
// Related:  shared/mask-ssn.pipe.ts, features/member/profile.ts (uses it)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { MaskSsnPipe } from './mask-ssn.pipe';

describe('MaskSsnPipe', () => {
  const pipe = new MaskSsnPipe();

  it('masks everything but the last 4', () => {
    expect(pipe.transform('1234')).toBe('***-**-1234');
  });

  it('returns an empty string when there is no value', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
    expect(pipe.transform('')).toBe('');
  });
});
