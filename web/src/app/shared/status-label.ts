// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     turns an enum-style status into readable text: "InReview" -> "In Review", "InfoRequested" -> "Info Requested"
// Layer:    shared (a pure function + a pipe that wraps it)
// Concepts: DRY: ONE formatter for every place a status appears as text (StatusBadge, the timeline's "from …").
//           the function is for TypeScript code (StatusBadge's computed); the PIPE is for templates ({{ s | statusLabel }}).
//           pure pipe (the default) = Angular only re-runs it when its input changes
// Related:  shared/status-badge.ts, features/member/claim-detail.ts, shared/models.ts (ClaimStatus, PaymentStatus)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Pipe, PipeTransform } from '@angular/core';

/** Splits a PascalCase status into words: "InReview" -> "In Review". */
export function statusLabel(status: string): string {
  // a lowercase letter directly followed by a capital -> put a space between them (g = every match)
  return status.replace(/([a-z])([A-Z])/g, '$1 $2');
}

@Pipe({ name: 'statusLabel' })
export class StatusLabelPipe implements PipeTransform {
  transform(status: string): string {
    return statusLabel(status);
  }
}
