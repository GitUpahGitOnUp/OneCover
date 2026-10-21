// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     "Statements": every premium the member owes/paid and every benefit paid to them
// Layer:    features/member (a page component)
// Concepts: Material TABLE, StatusBadge reused for payment statuses, the null-coalescing ?? in a template
//           (show "—" when there's no paid date yet), DatePipe w/ 'UTC' for date-only values
// Related:  core/payment.service.ts (getMine), shared/status-badge.ts, the API's GET /api/payments
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { PaymentService } from '../../core/payment.service';
import { Payment } from '../../shared/models';
import { StatusBadge } from '../../shared/status-badge';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-statements',
  imports: [CurrencyPipe, DatePipe, MatTableModule, StatusBadge],
  template: `
    <h1>Statements</h1>

    @if (loading()) {
      <p>Loading your statements…</p>
    } @else if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (payments().length === 0) {
      <p>No payments yet.</p>
    } @else {
      <table mat-table [dataSource]="payments()">
        <ng-container matColumnDef="due">
          <th mat-header-cell *matHeaderCellDef>Due</th>
          <td mat-cell *matCellDef="let p">{{ p.dueDate | date: 'mediumDate' : 'UTC' }}</td>
        </ng-container>
        <ng-container matColumnDef="description">
          <th mat-header-cell *matHeaderCellDef>Description</th>
          <td mat-cell *matCellDef="let p">{{ p.description }}</td>
        </ng-container>
        <ng-container matColumnDef="type">
          <th mat-header-cell *matHeaderCellDef>Type</th>
          <td mat-cell *matCellDef="let p">{{ p.type === 'Premium' ? 'You pay' : 'Paid to you' }}</td>
        </ng-container>
        <ng-container matColumnDef="amount">
          <th mat-header-cell *matHeaderCellDef>Amount</th>
          <td mat-cell *matCellDef="let p">{{ p.amount | currency }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let p"><app-status-badge [status]="p.status" /></td>
        </ng-container>
        <ng-container matColumnDef="paid">
          <th mat-header-cell *matHeaderCellDef>Paid on</th>
          <td mat-cell *matCellDef="let p">{{ (p.paidDate | date: 'mediumDate' : 'UTC') ?? '—' }}</td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns"></tr>
      </table>
    }
  `,
})
export class StatementsPage {
  private paymentService = inject(PaymentService);

  protected readonly columns = ['due', 'description', 'type', 'amount', 'status', 'paid'];
  protected readonly payments = signal<Payment[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.paymentService.getMine().subscribe({
      next: list => {
        this.payments.set(list);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(errorMessage(err, 'Could not load your statements.'));
        this.loading.set(false);
      },
    });
  }
}
