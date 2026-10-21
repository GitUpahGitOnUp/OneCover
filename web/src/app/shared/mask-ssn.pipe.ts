// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     a PIPE that displays the last 4 SSN digits as ***-**-1234
// Layer:    shared (usable in any template: {{ profile.ssnLast4 | maskSsn }})
// Concepts: a pipe = a small display transformer, used in templates w/ the | symbol. it's PURE by default:
//           Angular only re-runs it when its input changes, so it's cheap. implements the PipeTransform
//           INTERFACE (a contract: "I have a transform method"). tested on Day 9 (Vitest)
// Related:  features/member/profile.ts (uses it), User.SsnLast4 on the API (only the last 4 are stored)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'maskSsn' })
export class MaskSsnPipe implements PipeTransform {
  transform(last4: string | null | undefined): string {
    return last4 ? `***-**-${last4}` : '';   // nothing to show -> empty string, never "***-**-undefined"
  }
}
