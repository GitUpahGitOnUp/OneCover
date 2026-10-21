// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     "File a claim": a 3-step Material stepper (coverage -> dates + reason -> review + submit),
//           then a success panel showing the benefit the API calculated
// Layer:    features/member (a page component)
// Concepts: Material STEPPER in linear mode ([stepControl]: you can't move on until that step's form is valid),
//           two REACTIVE FORMS (one per step), a CROSS-FIELD validator on the dates group, SIGNALS for page state,
//           [min] on the start date so the picker can't go before coverage began (the API enforces it too),
//           Submit disabled while the request is in flight -> no duplicate claims from a double-click
// Related:  core/claim.service.ts, core/enrollment.service.ts, shared/validators.ts (endAfterStart),
//           the API's POST /api/claims + BenefitClaim.Submit + BenefitCalculator
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatStepperModule } from '@angular/material/stepper';
import { MatRadioModule } from '@angular/material/radio';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ClaimService } from '../../core/claim.service';
import { EnrollmentService } from '../../core/enrollment.service';
import { Claim, Enrollment } from '../../shared/models';
import { endAfterStart } from '../../shared/validators';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-file-claim',
  imports: [
    CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink, MatStepperModule, MatRadioModule,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatCardModule,
  ],
  template: `
    <h1>File a claim</h1>

    @if (submitted(); as claim) {
      <!-- success panel: shows what the API calculated -->
      <mat-card>
        <mat-card-header>
          <mat-card-title>Claim #{{ claim.id }} submitted</mat-card-title>
          <mat-card-subtitle>{{ claim.planName }} · status: {{ claim.status }}</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>Weekly benefit: <strong>{{ claim.weeklyBenefit | currency }}</strong></p>
          <p>Total for this claim: <strong>{{ claim.totalBenefit | currency }}</strong></p>
          <p>An administrator will review it. You can follow its status under My Claims.</p>
        </mat-card-content>
        <mat-card-actions>
          <a mat-flat-button [routerLink]="['/member/claims', claim.id]">View claim</a>
          <button mat-button type="button" (click)="startOver()">File another claim</button>
        </mat-card-actions>
      </mat-card>
    } @else if (loading()) {
      <p>Loading your coverage…</p>
    } @else if (activeEnrollments().length === 0) {
        <p>You need active disability coverage (short-term or long-term) before you can file a claim.</p>
      <a mat-flat-button routerLink="/member/plans">Browse plans</a>
    } @else {
      <!-- linear = each step must be valid before the next one opens -->
      <mat-stepper linear #stepper>
        <!-- STEP 1: which coverage -->
        <mat-step [stepControl]="coverageForm" label="Coverage">
          <form [formGroup]="coverageForm">
            <p id="coverage-label">Which coverage is this claim for?</p>
            <mat-radio-group formControlName="enrollmentId" aria-labelledby="coverage-label" class="choices">
              @for (e of activeEnrollments(); track e.id) {
                <mat-radio-button [value]="e.id">{{ e.planName }} ({{ e.planType }})</mat-radio-button>
              }
            </mat-radio-group>
            <div class="actions">
              <button mat-flat-button matStepperNext type="button">Next</button>
            </div>
          </form>
        </mat-step>

        <!-- STEP 2: dates + reason -->
        <mat-step [stepControl]="detailsForm" label="Details">
          <form [formGroup]="detailsForm">
            <mat-form-field class="full-width">
              <mat-label>First day of leave</mat-label>
              <input matInput type="date" formControlName="startDate" [min]="selectedEnrollment()?.enrolledOn ?? ''" />
              <mat-error>Start date is required (on or after your coverage began).</mat-error>
            </mat-form-field>

            <mat-form-field class="full-width">
              <mat-label>Last day of leave</mat-label>
              <input matInput type="date" formControlName="endDate" />
              <mat-error>End date is required.</mat-error>
            </mat-form-field>

            <!-- a GROUP-level error (from endAfterStart) isn't tied to one field, so it's shown here, not in mat-error -->
            @if (detailsForm.hasError('endBeforeStart') && detailsForm.controls.endDate.touched) {
              <p class="error" role="alert">The last day can't be before the first day.</p>
            }

            <mat-form-field class="full-width">
              <mat-label>Reason for leave</mat-label>
              <textarea matInput formControlName="reason" rows="3" maxlength="1000"></textarea>
              <mat-error>Please describe the reason.</mat-error>
            </mat-form-field>

            <div class="actions">
              <button mat-button matStepperPrevious type="button">Back</button>
              <button mat-flat-button matStepperNext type="button">Next</button>
            </div>
          </form>
        </mat-step>

        <!-- STEP 3: review + submit -->
        <mat-step label="Review">
          <dl class="review">
            <dt>Coverage</dt><dd>{{ selectedEnrollment()?.planName }}</dd>
            <dt>Dates</dt>
            <dd>
              {{ detailsForm.controls.startDate.value | date: 'mediumDate' : 'UTC' }} –
              {{ detailsForm.controls.endDate.value | date: 'mediumDate' : 'UTC' }}
            </dd>
            <dt>Reason</dt><dd>{{ detailsForm.controls.reason.value }}</dd>
          </dl>
          <p>Your benefit is calculated automatically when you submit.</p>
          <div class="actions">
            <button mat-button matStepperPrevious type="button" [disabled]="submitting()">Back</button>
            <!-- disabled while sending: a double-click can't file the same claim twice -->
            <button mat-flat-button type="button" (click)="submit()" [disabled]="submitting()">
              {{ submitting() ? 'Submitting…' : 'Submit claim' }}
            </button>
          </div>
        </mat-step>
      </mat-stepper>
    }
  `,
  styles: `
    .choices { display: flex; flex-direction: column; gap: 8px; margin-bottom: 16px; }
    .actions { display: flex; gap: 8px; margin-top: 8px; }
    .review { display: grid; grid-template-columns: max-content 1fr; gap: 8px 24px; }
    .review dt { font-weight: 500; }
    .review dd { margin: 0; }
  `,
})
export class FileClaimPage {
  private fb = inject(NonNullableFormBuilder);
  private claimService = inject(ClaimService);
  private enrollmentService = inject(EnrollmentService);
  private snackBar = inject(MatSnackBar);

  protected readonly activeEnrollments = signal<Enrollment[]>([]);
  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly submitted = signal<Claim | null>(null);   // set after a successful submit -> success panel

  // step 1's form: just the chosen enrollment
  protected readonly coverageForm = this.fb.group({
    enrollmentId: [null as number | null, Validators.required],
  });

  // step 2's form: dates + reason. endAfterStart is passed as a GROUP validator (it compares two fields)
  protected readonly detailsForm = this.fb.group(
    {
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      reason: ['', [Validators.required, Validators.maxLength(1000)]],
    },
    { validators: endAfterStart },
  );

  constructor() {
    this.enrollmentService.getMine().subscribe({
      next: list => {
        // only active DISABILITY coverage can be claimed against (same rule as the API's ClaimService)
        this.activeEnrollments.set(
          list.filter(e => e.status === 'Active' && (e.planType === 'STD' || e.planType === 'LTD')));
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.loading.set(false);
        this.snackBar.open(errorMessage(err, 'Could not load your coverage.'), 'OK', { duration: 5000 });
      },
    });
  }

  // the enrollment picked in step 1 (used for the review step + the start date's minimum)
  protected selectedEnrollment(): Enrollment | undefined {
    const id = this.coverageForm.controls.enrollmentId.value;
    return this.activeEnrollments().find(e => e.id === id);
  }

  protected submit() {
    if (this.coverageForm.invalid || this.detailsForm.invalid) {
      return;
    }
    this.submitting.set(true);

    const { startDate, endDate, reason } = this.detailsForm.getRawValue();   // destructuring: pull out 3 values at once
    this.claimService
      .submit({ enrollmentId: this.coverageForm.getRawValue().enrollmentId!, reason, startDate, endDate })
      .subscribe({
        next: claim => {
          this.submitted.set(claim);   // switch to the success panel
          this.submitting.set(false);
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          // e.g. "A claim can't start before the coverage began." straight from BenefitClaim.Submit
          this.snackBar.open(errorMessage(err, 'Could not submit your claim.'), 'OK', { duration: 6000 });
        },
      });
  }

  protected startOver() {
    this.submitted.set(null);
    this.coverageForm.reset();
    this.detailsForm.reset();
  }
}
