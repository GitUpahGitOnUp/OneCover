// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     a claim's details card + its status history timeline, shared by the member AND admin claim pages
// Layer:    shared (a presentational component: no services, no state, just displays the claim it's given)
// Concepts: input.required<Claim>() = the parent passes the claim in: <app-claim-summary [claim]="c" />.
//           input(false) = an OPTIONAL input w/ a default (only the admin page shows the member's name).
//           @let = a template variable, so the template reads c.id instead of calling claim() everywhere.
//           DRY + SINGLE RESPONSIBILITY: the pages handle loading/errors/actions; this only displays.
//           date-only values (leave dates) use 'UTC' so they never shift a day; timestamps show in local time
// Related:  features/member/claim-detail.ts, features/admin/claim-review.ts, shared/status-badge.ts,
//           shared/status-label.ts, shared/models.ts (Claim)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, input } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { Claim } from './models';
import { StatusBadge } from './status-badge';
import { StatusLabelPipe } from './status-label';

@Component({
  selector: 'app-claim-summary',
  imports: [CurrencyPipe, DatePipe, MatCardModule, StatusBadge, StatusLabelPipe],
  template: `
    @let c = claim();
    <mat-card>
      <mat-card-content>
        <dl class="details">
          @if (showMember()) {
            <dt>Member</dt><dd>{{ c.memberName }}</dd>
          }
          <dt>Coverage</dt><dd>{{ c.planName }} ({{ c.planType }})</dd>
          <dt>Leave dates</dt>
          <dd>{{ c.startDate | date: 'mediumDate' : 'UTC' }} – {{ c.endDate | date: 'mediumDate' : 'UTC' }}</dd>
          <dt>Reason</dt><dd>{{ c.reason }}</dd>
          <dt>Weekly benefit</dt><dd>{{ c.weeklyBenefit | currency }}</dd>
          <dt>Total benefit</dt><dd>{{ c.totalBenefit | currency }}</dd>
          <dt>Submitted</dt><dd>{{ c.submittedAt | date: 'medium' }}</dd>
        </dl>
      </mat-card-content>
    </mat-card>

    <h2>History</h2>
    <!-- an ordered list: the timeline's order matters, and screen readers announce "item 2 of 4" -->
    <ol class="timeline">
      @for (h of c.history; track $index) {
        <li>
          <app-status-badge [status]="h.toStatus" />
          @if (h.fromStatus) { <span class="from"> from {{ h.fromStatus | statusLabel }}</span> }
          <div class="meta">by {{ h.changedBy }} · {{ h.changedAt | date: 'medium' }}</div>
          @if (h.note) { <div class="note">"{{ h.note }}"</div> }
        </li>
      }
    </ol>
  `,
  // .details (the dl grid) is a global helper in styles.scss
  styles: `
    .timeline { list-style: none; padding-left: 16px; border-left: 2px solid var(--mat-sys-outline-variant); }
    .timeline li { margin: 0 0 16px 12px; }
    .from { color: var(--mat-sys-on-surface-variant); }
    .meta { font-size: 0.85em; color: var(--mat-sys-on-surface-variant); margin-top: 4px; }
    .note { font-style: italic; margin-top: 4px; }
  `,
})
export class ClaimSummary {
  readonly claim = input.required<Claim>();
  readonly showMember = input(false);   // the admin page turns this on; a member already knows it's theirs
}
