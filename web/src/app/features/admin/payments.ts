// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the admin payments page: every member's payments (filtered by status, Pending by default)
//           w/ "Mark paid" / "Mark failed" buttons on Pending rows
// Layer:    features/admin (a page component)
// Concepts: same load pattern as the claims queue (status SIGNAL -> toObservable -> switchMap cancels stale loads).
//           IMMUTABLE UPDATE: after a save, signal.update() + map() builds a NEW array w/ the one changed row,
//           so the signal sees a new reference and the table re-renders. per-row saving flag (savingId).
//           the buttons only appear on Pending rows, but the RULE lives in the API (Payment.MarkPaid/MarkFailed):
//           a non-Pending payment, or a paid date in the future, comes back as a 400 -> snackbar
// Related:  core/admin-payment.service.ts, shared/models.ts (Payment, PaymentStatus), shared/status-badge.ts,
//           the API's GET + PUT /api/admin/payments, Payment.MarkPaid / MarkFailed, member statements.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, of, switchMap, tap } from 'rxjs';
import { AdminPaymentService } from '../../core/admin-payment.service';
import { Payment, PaymentStatus } from '../../shared/models';
import { StatusBadge } from '../../shared/status-badge';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-admin-payments',
  imports: [CurrencyPipe, DatePipe, MatTableModule, MatFormFieldModule, MatSelectModule, MatButtonModule, StatusBadge],
  template: `
    <h1>Payments</h1>

    <mat-form-field>
      <mat-label>Status</mat-label>
      <mat-select [value]="status()" (selectionChange)="status.set($event.value)">
        <mat-option [value]="null">All statuses</mat-option>
        @for (s of statuses; track s) {
          <mat-option [value]="s">{{ s }}</mat-option>
        }
      </mat-select>
    </mat-form-field>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (loading()) {
      <p>Loading payments…</p>
    } @else if (payments().length === 0) {
      <p>No payments match this filter.</p>
    } @else {
      <table mat-table [dataSource]="payments()">
        <ng-container matColumnDef="member">
          <th mat-header-cell *matHeaderCellDef>Member</th>
          <td mat-cell *matCellDef="let p">{{ p.memberName }}</td>
        </ng-container>
        <ng-container matColumnDef="description">
          <th mat-header-cell *matHeaderCellDef>Description</th>
          <td mat-cell *matCellDef="let p">{{ p.description }}</td>
        </ng-container>
        <ng-container matColumnDef="type">
          <th mat-header-cell *matHeaderCellDef>Type</th>
          <!-- from the COMPANY's side: premiums come in, benefits go out -->
          <td mat-cell *matCellDef="let p">{{ p.type === 'Premium' ? 'Premium (in)' : 'Benefit (out)' }}</td>
        </ng-container>
        <ng-container matColumnDef="amount">
          <th mat-header-cell *matHeaderCellDef>Amount</th>
          <td mat-cell *matCellDef="let p">{{ p.amount | currency }}</td>
        </ng-container>
        <ng-container matColumnDef="due">
          <th mat-header-cell *matHeaderCellDef>Due</th>
          <td mat-cell *matCellDef="let p">{{ p.dueDate | date: 'mediumDate' : 'UTC' }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let p">
            <app-status-badge [status]="p.status" />
            @if (p.paidDate) { <div class="paid-on">{{ p.paidDate | date: 'mediumDate' : 'UTC' }}</div> }
          </td>
        </ng-container>
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef><span class="visually-hidden">Actions</span></th>
          <td mat-cell *matCellDef="let p">
            @if (p.status === 'Pending') {
              <!-- aria-label adds WHICH payment, so a screen reader doesn't hear 10 identical "Mark paid" buttons -->
              <button mat-button type="button" [disabled]="savingId() === p.id"
                      [attr.aria-label]="'Mark payment ' + p.id + ' paid'" (click)="mark(p, 'Paid')">
                Mark paid
              </button>
              <button mat-button type="button" [disabled]="savingId() === p.id"
                      [attr.aria-label]="'Mark payment ' + p.id + ' failed'" (click)="mark(p, 'Failed')">
                Mark failed
              </button>
            }
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns"></tr>
      </table>
    }
  `,
  styles: `
    .paid-on { font-size: 0.85em; color: var(--mat-sys-on-surface-variant); margin-top: 2px; }
    .visually-hidden { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
  `,
})
export class AdminPaymentsPage {
  private adminPayments = inject(AdminPaymentService);
  private snackBar = inject(MatSnackBar);

  protected readonly columns = ['member', 'description', 'type', 'amount', 'due', 'status', 'actions'];
  protected readonly statuses: PaymentStatus[] = ['Pending', 'Paid', 'Failed'];

  protected readonly status = signal<PaymentStatus | null>('Pending');   // the admin's to-do list by default
  protected readonly payments = signal<Payment[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly savingId = signal<number | null>(null);             // which row is being saved, if any

  constructor() {
    toObservable(this.status)
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap(status =>
          this.adminPayments.getAll(status).pipe(
            catchError((err: unknown) => {
              this.error.set(errorMessage(err, 'Could not load payments.'));
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(list => {
        if (list) {
          this.payments.set(list);
        }
        this.loading.set(false);
      });
  }

  protected mark(payment: Payment, status: 'Paid' | 'Failed') {
    this.savingId.set(payment.id);

    // paidOn: null -> the API uses today (and refuses future dates)
    this.adminPayments.updateStatus(payment.id, { status, paidOn: null }).subscribe({
      next: updated => {
        // swap in the API's version of this ONE row. map() returns a NEW array -> the signal notices the change
        this.payments.update(list => list.map(p => (p.id === updated.id ? updated : p)));
        this.savingId.set(null);
        this.snackBar.open(`Payment ${updated.id} marked ${updated.status.toLowerCase()}.`, 'OK', { duration: 4000 });
      },
      error: (err: unknown) => {
        this.savingId.set(null);
        this.snackBar.open(errorMessage(err, 'Could not update this payment.'), 'OK', { duration: 6000 });
      },
    });
  }
}
