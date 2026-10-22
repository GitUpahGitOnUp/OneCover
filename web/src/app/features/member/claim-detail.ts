// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the member's view of one claim: heading + status, then the shared details card + history timeline
// Layer:    features/member (a page component)
// Concepts: reading a ROUTE PARAMETER (/member/claims/:id) as an OBSERVABLE (route.paramMap), so going from
//           claim 3 to claim 7 without leaving the page still loads the new claim (Angular REUSES the component).
//           switchMap = "for each new id, start a new request and CANCEL the old one" (no stale response wins).
//           COMPOSITION: the display lives in the shared ClaimSummary; this page only loads + handles errors.
//           a member asking for someone else's claim gets the API's 404 -> "Claim 7 not found."
// Related:  core/claim.service.ts (getById), shared/claim-summary.ts, claims.ts, admin/claim-review.ts (the admin twin),
//           the API's GET /api/claims/{id}
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EMPTY, catchError, map, switchMap } from 'rxjs';
import { ClaimService } from '../../core/claim.service';
import { Claim } from '../../shared/models';
import { StatusBadge } from '../../shared/status-badge';
import { ClaimSummary } from '../../shared/claim-summary';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-claim-detail',
  imports: [RouterLink, StatusBadge, ClaimSummary],
  template: `
    <a routerLink="/member/claims">← Back to my claims</a>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (claim(); as c) {
      <h1>Claim #{{ c.id }} <app-status-badge [status]="c.status" /></h1>
      <app-claim-summary [claim]="c" />
    } @else {
      <p>Loading claim…</p>
    }
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
          // catchError INSIDE switchMap: a failed request ends only that request, not the paramMap stream
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
