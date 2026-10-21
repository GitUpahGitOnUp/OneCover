// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     one claim: its details, calculated benefit, and the full status history as a timeline
// Layer:    features/member (a page component)
// Concepts: reading a ROUTE PARAMETER (/member/claims/:id) via ActivatedRoute, a nullable signal (null = loading),
//           @for over the history (oldest first, as the API sends it), the reusable StatusBadge.
//           times: the API stores UTC, so they're shown in UTC and labeled that way (honest and unambiguous).
//           a member asking for someone else's claim gets the API's 404 -> "Claim 7 not found."
// Related:  core/claim.service.ts (getById), shared/status-badge.ts, claims.ts, the API's GET /api/claims/{id}
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { ClaimService } from '../../core/claim.service';
import { Claim } from '../../shared/models';
import { StatusBadge } from '../../shared/status-badge';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-claim-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatCardModule, StatusBadge],
  template: `
    <a routerLink="/member/claims">← Back to my claims</a>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (claim(); as c) {
      <h1>Claim #{{ c.id }} <app-status-badge [status]="c.status" /></h1>

      <mat-card>
        <mat-card-content>
          <dl class="details">
            <dt>Coverage</dt><dd>{{ c.planName }} ({{ c.planType }})</dd>
            <dt>Leave dates</dt>
            <dd>{{ c.startDate | date: 'mediumDate' : 'UTC' }} – {{ c.endDate | date: 'mediumDate' : 'UTC' }}</dd>
            <dt>Reason</dt><dd>{{ c.reason }}</dd>
            <dt>Weekly benefit</dt><dd>{{ c.weeklyBenefit | currency }}</dd>
            <dt>Total benefit</dt><dd>{{ c.totalBenefit | currency }}</dd>
            <dt>Submitted</dt><dd>{{ c.submittedAt | date: 'medium' : 'UTC' }} UTC</dd>
          </dl>
        </mat-card-content>
      </mat-card>

      <h2>History</h2>
      <!-- an ordered list: the timeline's order matters, and screen readers announce "item 2 of 4" -->
      <ol class="timeline">
        @for (h of c.history; track $index) {
          <li>
            <app-status-badge [status]="h.toStatus" />
            @if (h.fromStatus) { <span class="from"> from {{ h.fromStatus }}</span> }
            <div class="meta">by {{ h.changedBy }} · {{ h.changedAt | date: 'medium' : 'UTC' }} UTC</div>
            @if (h.note) { <div class="note">"{{ h.note }}"</div> }
          </li>
        }
      </ol>
    } @else {
      <p>Loading claim…</p>
    }
  `,
  styles: `
    .details { display: grid; grid-template-columns: max-content 1fr; gap: 8px 24px; }
    .details dt { font-weight: 500; }
    .details dd { margin: 0; }
    .timeline { list-style: none; padding-left: 16px; border-left: 2px solid var(--mat-sys-outline-variant); }
    .timeline li { margin: 0 0 16px 12px; }
    .from { color: var(--mat-sys-on-surface-variant); }
    .meta { font-size: 0.85em; color: var(--mat-sys-on-surface-variant); margin-top: 4px; }
    .note { font-style: italic; margin-top: 4px; }
  `,
})
export class ClaimDetailPage {
  private route = inject(ActivatedRoute);
  private claimService = inject(ClaimService);

  protected readonly claim = signal<Claim | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    // the :id from the URL arrives as a string, so Number() converts it
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.claimService.getById(id).subscribe({
      next: c => this.claim.set(c),
      error: (err: unknown) => this.error.set(errorMessage(err, 'Could not load this claim.')),
    });
  }
}
