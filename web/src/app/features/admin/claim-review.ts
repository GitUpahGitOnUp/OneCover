// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the admin's review page for one claim: the shared details + history, then a "Move to…" form
// Layer:    features/admin (a page component)
// Concepts: the dropdown only offers claim.nextStatuses (the API's ClaimWorkflow decides them), so the UI can't
//           even offer an invalid move. the API STILL enforces it: if a move is rejected (e.g. another admin moved
//           the claim first), its message is shown in a snackbar. after a move, the page shows the claim the API
//           sends back (new status, new history row, new nextStatuses), nothing is guessed client-side.
//           REACTIVE FORM (status required, note optional), COMPOSITION (shared ClaimSummary w/ showMember),
//           route param as an OBSERVABLE + switchMap (same pattern as the member's claim-detail)
// Related:  core/claim.service.ts (getById: admins may read any claim), core/admin-claim.service.ts (updateStatus),
//           shared/claim-summary.ts, the API's PUT /api/admin/claims/{id}/status -> BenefitClaim.MoveTo
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { EMPTY, catchError, map, switchMap } from 'rxjs';
import { ClaimService } from '../../core/claim.service';
import { AdminClaimService } from '../../core/admin-claim.service';
import { Claim, ClaimStatus } from '../../shared/models';
import { StatusBadge } from '../../shared/status-badge';
import { ClaimSummary } from '../../shared/claim-summary';
import { StatusLabelPipe, statusLabel } from '../../shared/status-label';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-claim-review',
  imports: [
    ReactiveFormsModule, RouterLink, MatFormFieldModule, MatSelectModule, MatInputModule, MatButtonModule,
    StatusBadge, ClaimSummary, StatusLabelPipe,
  ],
  template: `
    <a routerLink="/admin/claims">← Back to the queue</a>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (claim(); as c) {
      <h1>Claim #{{ c.id }} <app-status-badge [status]="c.status" /></h1>

      <h2>Move this claim</h2>
      @if (c.nextStatuses.length === 0) {
        <p>This claim is {{ c.status | statusLabel }}. No further moves are possible.</p>
      } @else {
        <form [formGroup]="moveForm" (ngSubmit)="move(c.id)">
          <mat-form-field>
            <mat-label>Move to</mat-label>
            <mat-select formControlName="status">
              <!-- ONLY the moves the workflow allows from the current status -->
              @for (s of c.nextStatuses; track s) {
                <mat-option [value]="s">{{ s | statusLabel }}</mat-option>
              }
            </mat-select>
            <mat-error>Pick a status.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Note (optional, shown in the history)</mat-label>
            <textarea matInput formControlName="note" rows="2" maxlength="1000"></textarea>
          </mat-form-field>

          <!-- disabled while saving: a double-click can't send the same move twice -->
          <button mat-flat-button type="submit" [disabled]="saving()">
            {{ saving() ? 'Saving…' : 'Move claim' }}
          </button>
        </form>
      }

      <app-claim-summary [claim]="c" [showMember]="true" />
    } @else {
      <p>Loading claim…</p>
    }
  `,
  styles: `form { margin-bottom: 24px; }`,
})
export class ClaimReviewPage {
  private route = inject(ActivatedRoute);
  private claimService = inject(ClaimService);
  private adminClaims = inject(AdminClaimService);
  private fb = inject(NonNullableFormBuilder);
  private snackBar = inject(MatSnackBar);

  protected readonly claim = signal<Claim | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected readonly moveForm = this.fb.group({
    status: [null as ClaimStatus | null, Validators.required],
    note: ['', Validators.maxLength(1000)],
  });

  constructor() {
    // same pattern as the member's claim-detail: react to the :id, cancel stale requests, reject junk ids
    this.route.paramMap
      .pipe(
        map(params => Number(params.get('id'))),
        switchMap(id => {
          this.claim.set(null);
          this.error.set(null);
          // a new claim starts w/ a clean form: a status picked for claim 3 must not carry over to claim 7
          this.moveForm.reset();
          this.saving.set(false);
          if (!Number.isInteger(id) || id <= 0) {
            this.error.set('Claim not found.');
            return EMPTY;
          }
          return this.claimService.getById(id).pipe(
            catchError((err: unknown) => {
              this.error.set(errorMessage(err, 'Could not load this claim.'));
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe(c => this.claim.set(c));
  }

  protected move(id: number) {
    if (this.moveForm.invalid) {
      this.moveForm.markAllAsTouched();   // show "Pick a status." if nothing was chosen
      return;
    }
    const { status, note } = this.moveForm.getRawValue();
    this.saving.set(true);

    // a blank note is sent as null (the API also turns blank notes into null, belt and braces)
    this.adminClaims.updateStatus(id, { status: status!, note: note.trim() || null }).subscribe({
      next: updated => {
        if (this.claim()?.id !== id) {
          return;   // the admin navigated to another claim while this saved: don't show claim 3 on claim 7's page
        }
        this.claim.set(updated);   // the API's version: new status, new history row, new nextStatuses
        this.moveForm.reset();
        this.saving.set(false);
        const extra = updated.status === 'Approved' && updated.totalBenefit > 0 ? ' A benefit payment is now pending.' : '';
        this.snackBar.open(`Claim moved to ${statusLabel(updated.status)}.${extra}`, 'OK', { duration: 5000 });
      },
      error: (err: unknown) => {
        if (this.claim()?.id === id) {
          this.saving.set(false);   // only un-stick the button for the claim this request was for
        }
        // e.g. "Can't move a claim from Approved to InReview." straight from ClaimWorkflow via BenefitClaim.MoveTo
        this.snackBar.open(errorMessage(err, 'Could not move this claim.'), 'OK', { duration: 6000 });
      },
    });
  }
}
