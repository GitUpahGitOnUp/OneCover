// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     "File a claim": a 3-step Material stepper (coverage -> dates + reason -> review + submit),
//           then a success panel showing the benefit the API calculated
// Layer:    features/member (a page component)
// Concepts: Material STEPPER in linear mode ([stepControl]: you can't move on until that step's form is valid),
//           two REACTIVE FORMS (one per step), CROSS-FIELD validators on the dates group, SIGNALS for page state.
//           toSignal() turns the radio's valueChanges OBSERVABLE into a signal, so selectedEnrollment is a COMPUTED
//           (cached, recalculated only when the pick changes). the start date must be on/after that coverage's start:
//           [min] guides the picker, a VALIDATOR actually blocks typed dates (both mirror BenefitClaim.Submit).
//           an effect() re-checks the start date whenever the picked coverage changes.
//           Submit disabled while the request is in flight -> no duplicate claims from a double-click
// Related:  core/claim.service.ts, core/enrollment.service.ts, shared/validators.ts,
//           the API's POST /api/claims + BenefitClaim.Submit + BenefitCalculator
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
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
import { endAfterStart, startOnOrAfter, withinOneYear } from '../../shared/validators';
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
    } @else if (loadError()) {
      <!-- checked BEFORE "no coverage": a failed load must not tell the member they have no coverage -->
      <p class="error" role="alert">{{ loadError() }}</p>
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
              @if (selectedEnrollment(); as e) {
                <mat-hint>Your coverage began {{ e.enrolledOn | date: 'mediumDate' : 'UTC' }}.</mat-hint>
              }
              <!-- one mat-error, message picked by WHICH error the control has -->
              <mat-error>
                @if (detailsForm.controls.startDate.hasError('beforeCoverage')) {
                  The first day can't be before your coverage began.
                } @else {
                  Start date is required.
                }
              </mat-error>
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
            @if (detailsForm.hasError('tooLong') && detailsForm.controls.endDate.touched) {
              <p class="error" role="alert">A single claim can't cover more than one year.</p>
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
          <dl class="details">
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
  // .details (the review step's dl grid) is a global helper in styles.scss
  styles: `
    .choices { display: flex; flex-direction: column; gap: 8px; margin-bottom: 16px; }
    .actions { display: flex; gap: 8px; margin-top: 8px; }
  `,
})
export class FileClaimPage {
  private fb = inject(NonNullableFormBuilder);
  private claimService = inject(ClaimService);
  private enrollmentService = inject(EnrollmentService);
  private snackBar = inject(MatSnackBar);

  protected readonly activeEnrollments = signal<Enrollment[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);   // set if the coverage list fails to load
  protected readonly submitting = signal(false);
  protected readonly submitted = signal<Claim | null>(null);   // set after a successful submit -> success panel

  // step 1's form: just the chosen enrollment
  protected readonly coverageForm = this.fb.group({
    enrollmentId: [null as number | null, Validators.required],
  });

  // the radio's value as a SIGNAL. valueChanges is an observable; toSignal subscribes for us (and unsubscribes
  // when the page is destroyed). initialValue covers the moment before anything is picked
  private readonly enrollmentId = toSignal(this.coverageForm.controls.enrollmentId.valueChanges, {
    initialValue: null,
  });

  // the enrollment picked in step 1 (review step, the start date's hint/minimum, the validator below).
  // declared BEFORE detailsForm on purpose: the form runs its validators as soon as it's created
  protected readonly selectedEnrollment = computed(() =>
    this.activeEnrollments().find(e => e.id === this.enrollmentId()));

  // step 2's form: dates + reason. the date-pair rules are GROUP validators (they compare two fields);
  // startOnOrAfter is on the one control, and reads the current minimum each time it runs
  protected readonly detailsForm = this.fb.group(
    {
      startDate: ['', [Validators.required, startOnOrAfter(() => this.selectedEnrollment()?.enrolledOn)]],
      endDate: ['', Validators.required],
      reason: ['', [Validators.required, Validators.maxLength(1000)]],
    },
    { validators: [endAfterStart, withinOneYear] },
  );

  constructor() {
    // validators only re-run when THEIR control changes. if the member goes back and picks a different
    // coverage, the start date must be re-checked against the new minimum. effect() re-runs whenever a
    // signal it reads (selectedEnrollment) changes
    effect(() => {
      this.selectedEnrollment();
      this.detailsForm.controls.startDate.updateValueAndValidity();
    });

    this.enrollmentService.getMine().subscribe({
      next: list => {
        // only active DISABILITY coverage can be claimed against (same rule as the API's Plan.IsDisability)
        this.activeEnrollments.set(
          list.filter(e => e.status === 'Active' && (e.planType === 'STD' || e.planType === 'LTD')));
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.loadError.set(errorMessage(err, 'Could not load your coverage.'));
        this.loading.set(false);
      },
    });
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
