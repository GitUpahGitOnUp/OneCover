// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     one claim: its details, calculated benefit, and the full status history as a timeline
// Layer:    features/member (a page component)
// Concepts: reading a ROUTE PARAMETER (/member/claims/:id) as an OBSERVABLE (route.paramMap), so going from
//           claim 3 to claim 7 without leaving the page still loads the new claim (Angular REUSES the component).
//           switchMap = "for each new id, start a new request and CANCEL the old one" (no stale response wins).
//           a nullable signal (null = loading), @for over the history (oldest first, as the API sends it),
//           the reusable StatusBadge + statusLabel pipe.
//           times: the API sends UTC with a "Z", so the date pipe shows them in the member's LOCAL time zone.
//           a member asking for someone else's claim gets the API's 404 -> "Claim 7 not found."
// Related:  core/claim.service.ts (getById), shared/status-badge.ts, shared/status-label.ts, claims.ts,
//           the API's GET /api/claims/{id}, Data/UtcDateTimeConverter.cs (why the "Z" is there)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { EMPTY, catchError, map, switchMap } from 'rxjs';
import { ClaimService } from '../../core/claim.service';
import { Claim } from '../../shared/models';
import { StatusBadge } from '../../shared/status-badge';
import { StatusLabelPipe } from '../../shared/status-label';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-claim-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatCardModule, StatusBadge, StatusLabelPipe],
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
            <!-- date-only values ("2026-10-01") stay in 'UTC' so they never shift a day; timestamps below use local time -->
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
    } @else {
      <p>Loading claim…</p>
    }
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
export class ClaimDetailPage {
  private route = inject(ActivatedRoute);
  private claimService = inject(ClaimService);

  protected readonly claim = signal<Claim | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.route.paramMap
      .pipe(
        // the :id arrives as a string ("7", or junk like "abc" if someone edits the URL)
        map(params => Number(params.get('id'))),
        switchMap(id => {
          this.claim.set(null);   // back to "Loading…" while the new claim loads
          this.error.set(null);
          if (!Number.isInteger(id) || id <= 0) {
            this.error.set('Claim not found.');   // no point asking the API for claim "abc"
            return EMPTY;                         // EMPTY = an observable that completes w/o emitting anything
          }
          // catchError INSIDE switchMap: a failed request ends only that request, not the paramMap stream,
          // so the page still reacts to the next id
          return this.claimService.getById(id).pipe(
            catchError((err: unknown) => {
              this.error.set(errorMessage(err, 'Could not load this claim.'));
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(),   // unsubscribe automatically when the page is destroyed (no memory leak)
      )
      .subscribe(c => this.claim.set(c));
  }
}
