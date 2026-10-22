// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the add/edit plan popup. no plan passed in = "Add a plan"; a plan passed in = "Edit plan" (pre-filled)
// Layer:    features/admin (a dialog component, opened by plans-admin.ts)
// Concepts: MatDialog: MAT_DIALOG_DATA = the plan handed in by dialog.open() (an INJECTION TOKEN, DI again),
//           MatDialogRef.close(saved) = hand the saved plan back to the page (it arrives in afterClosed()).
//           the dialog SAVES the plan itself, so an API error shows here and the admin keeps what they typed.
//           REACTIVE FORM mirroring SavePlanRequest's validation (validate at the edge, the API checks again).
//           percent typed as a WHOLE number (60) and sent as a fraction (0.60): BenefitPercent is decimal(18,2),
//           so 66.7% would be silently rounded. disability fields only show for STD/LTD (0 for everything else)
// Related:  plans-admin.ts (opens it), core/plan.service.ts (create/update), shared/models.ts (Plan, SavePlanRequest),
//           the API's SavePlanRequest + PlansController (POST / PUT /api/plans)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { PlanService } from '../../core/plan.service';
import { Plan, PlanType, SavePlanRequest } from '../../shared/models';
import { errorMessage } from '../../shared/problem-details';

const WHOLE_NUMBER = /^\d+$/;   // digits only: no decimals, no minus sign

@Component({
  selector: 'app-plan-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ plan ? 'Edit plan' : 'Add a plan' }}</h2>

    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        <mat-form-field class="full-width">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" maxlength="100" />
          <mat-error>Name is required (100 characters max).</mat-error>
        </mat-form-field>

        <mat-form-field class="full-width">
          <mat-label>Type</mat-label>
          <mat-select formControlName="type">
            @for (t of types; track t) {
              <mat-option [value]="t">{{ t }}</mat-option>
            }
          </mat-select>
          <mat-error>Pick a plan type.</mat-error>
        </mat-form-field>

        <mat-form-field class="full-width">
          <mat-label>Description (optional)</mat-label>
          <textarea matInput formControlName="description" rows="2" maxlength="1000"></textarea>
        </mat-form-field>

        <mat-form-field class="full-width">
          <mat-label>Monthly premium ($)</mat-label>
          <input matInput type="number" formControlName="monthlyPremium" min="0" max="10000" step="0.01" />
          <mat-error>Enter an amount from 0 to 10,000.</mat-error>
        </mat-form-field>

        @if (isDisability()) {
          <mat-form-field class="full-width">
            <mat-label>Benefit (% of weekly salary)</mat-label>
            <input matInput type="number" formControlName="benefitPercent" min="0" max="100" step="1" />
            <mat-error>Enter a whole percent from 0 to 100.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Weekly maximum ($)</mat-label>
            <input matInput type="number" formControlName="weeklyMaximum" min="0" max="100000" step="0.01" />
            <mat-error>Enter an amount from 0 to 100,000.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Waiting period (days)</mat-label>
            <input matInput type="number" formControlName="eliminationDays" min="0" max="365" step="1" />
            <mat-error>Enter whole days from 0 to 365.</mat-error>
          </mat-form-field>
        } @else {
          <p class="hint">Benefit percent, weekly maximum and waiting period only apply to STD and LTD plans.</p>
        }

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <!-- mat-dialog-close = close w/o a result (afterClosed() emits undefined -> the page does nothing) -->
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save' }}</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `.hint { color: var(--mat-sys-on-surface-variant); }`,
})
export class PlanDialog {
  private fb = inject(NonNullableFormBuilder);
  private planService = inject(PlanService);
  private dialogRef = inject<MatDialogRef<PlanDialog, Plan>>(MatDialogRef);   // <the dialog, what close() returns>

  // the plan to edit, or null for "add". comes from dialog.open(PlanDialog, { data: plan })
  protected readonly plan = inject<Plan | null>(MAT_DIALOG_DATA);

  protected readonly types: PlanType[] = ['STD', 'LTD', 'Life', 'Accident', 'Dental'];
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  // pre-filled when editing. same limits as the API's SavePlanRequest
  protected readonly form = this.fb.group({
    name: [this.plan?.name ?? '', [Validators.required, Validators.maxLength(100)]],
    type: [this.plan?.type ?? (null as PlanType | null), Validators.required],
    description: [this.plan?.description ?? '', Validators.maxLength(1000)],
    monthlyPremium: [this.plan?.monthlyPremium ?? (null as number | null),
      [Validators.required, Validators.min(0), Validators.max(10000)]],
    // stored as a fraction (0.60), edited as a whole percent (60)
    benefitPercent: [this.plan ? Math.round(this.plan.benefitPercent * 100) : 0,
      [Validators.required, Validators.min(0), Validators.max(100), Validators.pattern(WHOLE_NUMBER)]],
    weeklyMaximum: [this.plan?.weeklyMaximum ?? 0, [Validators.required, Validators.min(0), Validators.max(100000)]],
    eliminationDays: [this.plan?.eliminationDays ?? 0,
      [Validators.required, Validators.min(0), Validators.max(365), Validators.pattern(WHOLE_NUMBER)]],
  });

  // the type dropdown as a signal -> a computed "show the disability fields?"
  private readonly type = toSignal(this.form.controls.type.valueChanges, { initialValue: this.form.controls.type.value });
  protected readonly isDisability = computed(() => this.type() === 'STD' || this.type() === 'LTD');

  constructor() {
    // switching to a non-disability type zeroes the hidden fields, so a stale invalid value
    // (e.g. 150%) can't block Save from a field the admin can no longer see
    this.form.controls.type.valueChanges.pipe(takeUntilDestroyed()).subscribe(type => {
      if (type !== 'STD' && type !== 'LTD') {
        this.form.patchValue({ benefitPercent: 0, weeklyMaximum: 0, eliminationDays: 0 });
      }
    });
  }

  protected save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();   // reveal every field's error at once
      return;
    }
    const v = this.form.getRawValue();
    const request: SavePlanRequest = {
      name: v.name.trim(),
      type: v.type!,
      description: v.description.trim() || null,
      monthlyPremium: v.monthlyPremium!,
      benefitPercent: v.benefitPercent / 100,   // 60 -> 0.6
      weeklyMaximum: v.weeklyMaximum,
      eliminationDays: v.eliminationDays,
    };

    this.saving.set(true);
    this.error.set(null);

    // same form, two endpoints: PUT when editing, POST when adding. both return the saved Plan
    // $ === naming convention for 'this is an observable'
    const save$ = this.plan ? this.planService.update(this.plan.id, request) : this.planService.create(request);
    save$.subscribe({
      next: saved => this.dialogRef.close(saved),   // hand it back to the page
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(errorMessage(err, 'Could not save the plan.'));   // stays open, nothing typed is lost
      },
    });
  }
}
