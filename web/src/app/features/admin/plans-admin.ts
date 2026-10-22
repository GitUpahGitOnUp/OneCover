// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the admin plans page: active plans in a table, "Add plan", and Edit / Retire on each row
// Layer:    features/admin (a page component)
// Concepts: opening a MatDialog + reading its result via afterClosed() (an OBSERVABLE that emits once),
//           IMMUTABLE list updates on signals (filter/spread/sort build a NEW array), a confirm step before retiring.
//           retire = a SOFT delete in the API (IsActive = false): the plan leaves this list (GET /api/plans only
//           returns active plans) and members stop seeing it, but existing enrollments still point to it
// Related:  plan-dialog.ts, core/plan.service.ts (getActive, retire), the API's PlansController, member plans.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, PercentPipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PlanService } from '../../core/plan.service';
import { Plan } from '../../shared/models';
import { errorMessage } from '../../shared/problem-details';
import { PlanDialog } from './plan-dialog';

@Component({
  selector: 'app-plans-admin',
  imports: [CurrencyPipe, PercentPipe, MatTableModule, MatButtonModule],
  template: `
    <div class="heading">
      <h1>Manage plans</h1>
      <button mat-flat-button type="button" (click)="openDialog(null)">Add plan</button>
    </div>
    <p class="hint">Retiring a plan hides it from members. Existing enrollments are kept.</p>

    @if (loading()) {
      <p>Loading plans…</p>
    } @else if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (plans().length === 0) {
      <p>No active plans. Add one to get started.</p>
    } @else {
      <table mat-table [dataSource]="plans()">
        <ng-container matColumnDef="name">
          <th mat-header-cell *matHeaderCellDef>Name</th>
          <td mat-cell *matCellDef="let p">{{ p.name }}</td>
        </ng-container>
        <ng-container matColumnDef="type">
          <th mat-header-cell *matHeaderCellDef>Type</th>
          <td mat-cell *matCellDef="let p">{{ p.type }}</td>
        </ng-container>
        <ng-container matColumnDef="premium">
          <th mat-header-cell *matHeaderCellDef>Premium</th>
          <td mat-cell *matCellDef="let p">{{ p.monthlyPremium | currency }}/mo</td>
        </ng-container>
        <ng-container matColumnDef="benefit">
          <th mat-header-cell *matHeaderCellDef>Benefit</th>
          <td mat-cell *matCellDef="let p">
            @if (p.type === 'STD' || p.type === 'LTD') {
              {{ p.benefitPercent | percent }} · up to {{ p.weeklyMaximum | currency }}/wk · {{ p.eliminationDays }}-day wait
            } @else {
              —
            }
          </td>
        </ng-container>
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef><span class="visually-hidden">Actions</span></th>
          <td mat-cell *matCellDef="let p">
            <button mat-button type="button" [attr.aria-label]="'Edit ' + p.name" (click)="openDialog(p)">Edit</button>
            <button mat-button type="button" [attr.aria-label]="'Retire ' + p.name"
                    [disabled]="retiringId() === p.id" (click)="retire(p)">
              Retire
            </button>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns"></tr>
      </table>
    }
  `,
  styles: `
    .heading { display: flex; align-items: center; justify-content: space-between; }
    .hint { color: var(--mat-sys-on-surface-variant); }
    .visually-hidden { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
  `,
})
export class PlansAdminPage {
  private planService = inject(PlanService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  protected readonly columns = ['name', 'type', 'premium', 'benefit', 'actions'];
  protected readonly plans = signal<Plan[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly retiringId = signal<number | null>(null);

  constructor() {
    this.planService.getActive().subscribe({
      next: list => {
        this.plans.set(list);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(errorMessage(err, 'Could not load plans.'));
        this.loading.set(false);
      },
    });
  }

  /** plan = null -> "Add a plan"; a plan -> "Edit plan". */
  protected openDialog(plan: Plan | null) {
    // generics: <the dialog component, the data it receives, what it returns on close>
    this.dialog
      .open<PlanDialog, Plan | null, Plan>(PlanDialog, { data: plan, width: '560px' })
      .afterClosed()
      .subscribe(saved => {
        if (!saved) {
          return;   // cancelled
        }
        // drop the old copy (if editing), add the saved one, keep the list sorted by name like the API
        this.plans.update(list =>
          [...list.filter(p => p.id !== saved.id), saved].sort((a, b) => a.name.localeCompare(b.name)));
        this.snackBar.open(plan ? `${saved.name} updated.` : `${saved.name} added.`, 'OK', { duration: 4000 });
      });
  }

  protected retire(plan: Plan) {
    // the browser's built-in confirm box: simple, keyboard-accessible, and enough for a one-line yes/no
    if (!confirm(`Retire "${plan.name}"? Members won't be able to enroll in it anymore.`)) {
      return;
    }
    this.retiringId.set(plan.id);

    this.planService.retire(plan.id).subscribe({
      next: () => {
        this.plans.update(list => list.filter(p => p.id !== plan.id));   // retired plans aren't "active" anymore
        this.retiringId.set(null);
        this.snackBar.open(`${plan.name} retired.`, 'OK', { duration: 4000 });
      },
      error: (err: unknown) => {
        this.retiringId.set(null);
        this.snackBar.open(errorMessage(err, 'Could not retire this plan.'), 'OK', { duration: 6000 });
      },
    });
  }
}
