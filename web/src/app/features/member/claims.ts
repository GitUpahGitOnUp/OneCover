// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     "My Claims": the member's claims in a Material table, each linking to its detail page
// Layer:    features/member (a page component)
// Concepts: Material TABLE, a reusable child component (<app-status-badge>) w/ an input, [routerLink] w/ an array
//           (['/member/claims', c.id] builds /member/claims/5), DatePipe w/ 'UTC' for date-only values.
//           the link sits on the claim number (a real <a>), so it works w/ the keyboard + screen readers
// Related:  core/claim.service.ts (getMine), shared/status-badge.ts, claim-detail.ts, file-claim.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { ClaimService } from '../../core/claim.service';
import { Claim } from '../../shared/models';
import { StatusBadge } from '../../shared/status-badge';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-claims',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatTableModule, MatButtonModule, StatusBadge],
  template: `
    <div class="heading">
      <h1>My claims</h1>
      <a mat-flat-button routerLink="/member/claims/new">File a claim</a>
    </div>

    @if (loading()) {
      <p>Loading your claims…</p>
    } @else if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (claims().length === 0) {
      <p>You haven't filed any claims yet.</p>
    } @else {
      <table mat-table [dataSource]="claims()">
        <ng-container matColumnDef="id">
          <th mat-header-cell *matHeaderCellDef>Claim</th>
          <td mat-cell *matCellDef="let c"><a [routerLink]="['/member/claims', c.id]">#{{ c.id }}</a></td>
        </ng-container>
        <ng-container matColumnDef="plan">
          <th mat-header-cell *matHeaderCellDef>Coverage</th>
          <td mat-cell *matCellDef="let c">{{ c.planName }}</td>
        </ng-container>
        <ng-container matColumnDef="dates">
          <th mat-header-cell *matHeaderCellDef>Leave dates</th>
          <td mat-cell *matCellDef="let c">
            {{ c.startDate | date: 'mediumDate' : 'UTC' }} – {{ c.endDate | date: 'mediumDate' : 'UTC' }}
          </td>
        </ng-container>
        <ng-container matColumnDef="total">
          <th mat-header-cell *matHeaderCellDef>Total benefit</th>
          <td mat-cell *matCellDef="let c">{{ c.totalBenefit | currency }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let c"><app-status-badge [status]="c.status" /></td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns"></tr>
      </table>
    }
  `,
})
export class ClaimsPage {
  private claimService = inject(ClaimService);

  protected readonly columns = ['id', 'plan', 'dates', 'total', 'status'];
  protected readonly claims = signal<Claim[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.claimService.getMine().subscribe({
      next: list => {
        this.claims.set(list);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(errorMessage(err, 'Could not load your claims.'));
        this.loading.set(false);
      },
    });
  }
}
