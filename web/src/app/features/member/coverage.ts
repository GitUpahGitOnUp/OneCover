// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     "My Coverage": the member's enrollments in a Material table, w/ a friendly empty state
// Layer:    features/member (a page component)
// Concepts: Material TABLE (columns declared once, rows rendered from an array), SIGNALS for page state,
//           pipes (currency, date). DatePipe w/ 'UTC': the API sends dates like "2026-10-18"; JavaScript reads those
//           as midnight UTC, so without 'UTC' a US browser would show Oct 17. one shared error helper (errorMessage)
// Related:  core/enrollment.service.ts (getMine), shared/models.ts (Enrollment), member.routes.ts (/member/coverage)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { EnrollmentService } from '../../core/enrollment.service';
import { Enrollment } from '../../shared/models';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-coverage',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatTableModule, MatButtonModule],
  template: `
    <h1>My coverage</h1>

    @if (loading()) {
      <p>Loading your coverage…</p>
    } @else if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (enrollments().length === 0) {
      <p>You're not enrolled in any plans yet.</p>
      <a mat-flat-button routerLink="/member/plans">Browse plans</a>
    } @else {
      <!-- each ng-container matColumnDef = one column: its header cell + the cell shown on every row -->
      <table mat-table [dataSource]="enrollments()">
        <ng-container matColumnDef="plan">
          <th mat-header-cell *matHeaderCellDef>Plan</th>
          <td mat-cell *matCellDef="let e">{{ e.planName }}</td>
        </ng-container>
        <ng-container matColumnDef="type">
          <th mat-header-cell *matHeaderCellDef>Type</th>
          <td mat-cell *matCellDef="let e">{{ e.planType }}</td>
        </ng-container>
        <ng-container matColumnDef="premium">
          <th mat-header-cell *matHeaderCellDef>Monthly premium</th>
          <td mat-cell *matCellDef="let e">{{ e.monthlyPremium | currency }}</td>
        </ng-container>
        <ng-container matColumnDef="enrolledOn">
          <th mat-header-cell *matHeaderCellDef>Coverage start</th>
          <td mat-cell *matCellDef="let e">{{ e.enrolledOn | date: 'mediumDate' : 'UTC' }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let e">{{ e.status }}</td>
        </ng-container>

        <!-- which columns to show, in order -->
        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns"></tr>
      </table>
    }
  `,
})
export class CoveragePage {
  private enrollmentService = inject(EnrollmentService);

  protected readonly columns = ['plan', 'type', 'premium', 'enrolledOn', 'status'];
  protected readonly enrollments = signal<Enrollment[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.enrollmentService.getMine().subscribe({
      next: list => {
        this.enrollments.set(list);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(errorMessage(err, 'Could not load your coverage.'));
        this.loading.set(false);
      },
    });
  }
}
