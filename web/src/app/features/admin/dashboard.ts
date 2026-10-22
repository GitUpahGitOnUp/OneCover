// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the admin dashboard (/admin): total claims + one count card per claim status, w/ a link to the queue
// Layer:    features/admin (a page component; admins land here after login)
// Concepts: one GET -> a signal -> @for over the cards. the API lists EVERY status (zeros included), so the
//           template never handles a "missing" status (a Day 5 DTO decision paying off).
//           statusLabel pipe for the text ("In Review"); the counting itself is a LINQ GroupBy in the API
//           (ClaimRepository.CountByStatusAsync), so only 6 small numbers cross the network, not every claim
//           each card links to the queue pre-filtered via a QUERY PARAMETER (?status=Submitted).
// Related:  core/admin-claim.service.ts (getDashboard), shared/status-label.ts, claims-queue.ts,
//           the API's GET /api/admin/dashboard (DashboardDto / StatusCountDto)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { AdminClaimService } from '../../core/admin-claim.service';
import { Dashboard } from '../../shared/models';
import { StatusLabelPipe } from '../../shared/status-label';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, MatCardModule, MatButtonModule, StatusLabelPipe],
  template: `
    <div class="heading">
      <h1>Admin dashboard</h1>
      <a mat-flat-button routerLink="/admin/claims">Open claims queue</a>
    </div>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (dashboard(); as d) {
      <p class="total">{{ d.totalClaims }} claims in total</p>

      <!-- a list of cards: screen readers announce "list, 6 items", then each status + count -->
      <ul class="cards">
        @for (s of d.claimsByStatus; track s.status) {
          <li>
            <!-- a real <a>, not a (click) on the card: works w/ Tab + Enter, middle-click, and screen readers.
                 [queryParams] builds ?status=Submitted -->
            <a class="card-link" routerLink="/admin/claims" [queryParams]="{ status: s.status }">
              <mat-card [class]="'card ' + s.status.toLowerCase()">
                <mat-card-content>
                  <div class="count">{{ s.count }}</div>
                  <div class="label">{{ s.status | statusLabel }}</div>
                </mat-card-content>
              </mat-card>
            </a>
          </li>

        }
      </ul>
    } @else {
      <p>Loading dashboard…</p>
    }
  `,
  // same light backgrounds as StatusBadge (dark text keeps WCAG AA contrast)
  styles: `
    .heading { display: flex; align-items: center; justify-content: space-between; }
    .total { color: var(--mat-sys-on-surface-variant); }
    .cards { list-style: none; padding: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 16px; }
    .card { text-align: center; }
    .count { font-size: 2.25rem; font-weight: 600; line-height: 1.2; }
    .label { margin-top: 4px; font-weight: 500; }
    .submitted     { background: #e3f2fd; color: #0d47a1; }
    .inreview      { background: #fff3e0; color: #8a3a00; }
    .inforequested { background: #fce4ec; color: #880e4f; }
    .approved      { background: #e8f5e9; color: #1b5e20; }
    .denied        { background: #ffebee; color: #b71c1c; }
    .closed        { background: #eeeeee; color: #424242; }
    .card-link { display: block; text-decoration: none; color: inherit; border-radius: 12px; }
    .card-link:hover .card { box-shadow: 0 2px 8px rgb(0 0 0 / 0.2); }
    .card-link:focus-visible { outline: 2px solid var(--mat-sys-primary); outline-offset: 2px; }   /* keyboard focus ring */
  `,
})
export class DashboardPage {
  private adminClaims = inject(AdminClaimService);

  protected readonly dashboard = signal<Dashboard | null>(null);   // null = still loading
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.adminClaims.getDashboard().subscribe({
      next: d => this.dashboard.set(d),
      error: (err: unknown) => this.error.set(errorMessage(err, 'Could not load the dashboard.')),
    });
  }
}
