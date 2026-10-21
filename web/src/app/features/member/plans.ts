// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the member's plans page: every active plan as a card, w/ an Enroll button
//           (or an "Enrolled" badge for plans they already have)
// Layer:    features/member (a page component)
// Concepts: SIGNALS for page state (set + update), RxJS forkJoin (run 2 requests in parallel,
//           continue when BOTH finish), @for w/ track (efficient list rendering), Angular pipes (currency, percent),
//           MatSnackBar for feedback, showing the API's ProblemDetails "detail" on errors
// Related:  core/plan.service.ts, core/enrollment.service.ts, member.routes.ts, the API's Plans/Enrollments controllers
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, PercentPipe } from '@angular/common';
import { errorMessage } from '../../shared/problem-details';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PlanService } from '../../core/plan.service';
import { EnrollmentService } from '../../core/enrollment.service';
import { Plan } from '../../shared/models';

@Component({
  selector: 'app-plans',
  imports: [CurrencyPipe, PercentPipe, MatCardModule, MatButtonModule],
  template: `
    <h1>Coverage plans</h1>

    @if (loading()) {
      <p>Loading plans…</p>
    } @else if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else {
      <div class="plan-grid">
        <!-- track plan.id: Angular reuses the right card when the list changes instead of redrawing them all -->
        @for (plan of plans(); track plan.id) {
          <mat-card>
            <mat-card-header>
              <mat-card-title>{{ plan.name }}</mat-card-title>
              <mat-card-subtitle>{{ plan.type }} · {{ plan.monthlyPremium | currency }}/month</mat-card-subtitle>
            </mat-card-header>

            <mat-card-content>
              <p>{{ plan.description }}</p>

              <!-- the disability numbers only mean something for STD/LTD plans -->
              @if (plan.type === 'STD' || plan.type === 'LTD') {
                <ul class="plan-facts">
                  <li>Pays {{ plan.benefitPercent | percent }} of your weekly salary</li>
                  <li>Up to {{ plan.weeklyMaximum | currency }} a week</li>
                  <li>{{ plan.eliminationDays }}-day waiting period</li>
                </ul>
              }
            </mat-card-content>

            <mat-card-actions>
              @if (enrolledPlanIds().has(plan.id)) {
                <button mat-stroked-button type="button" disabled>✓ Enrolled</button>
              } @else {
                <button mat-flat-button type="button"
                        [disabled]="enrollingId() !== null"
                        (click)="enroll(plan)">
                  {{ enrollingId() === plan.id ? 'Enrolling…' : 'Enroll' }}
                </button>
              }
            </mat-card-actions>
          </mat-card>
        }
      </div>
    }
  `,
  styles: `
    .plan-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; }
    .plan-facts { padding-left: 20px; margin: 8px 0 0; }
  `,
})
export class PlansPage {
  private planService = inject(PlanService);
  private enrollmentService = inject(EnrollmentService);
  private snackBar = inject(MatSnackBar);

  protected readonly plans = signal<Plan[]>([]);
  protected readonly enrolledPlanIds = signal<Set<number>>(new Set());   // which plans this member already has
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly enrollingId = signal<number | null>(null);          // the plan currently being enrolled in

  constructor() {
    // forkJoin: send BOTH requests at once, and run next() when both have answered
    forkJoin({
      plans: this.planService.getActive(),
      enrollments: this.enrollmentService.getMine(),
    }).subscribe({
      next: ({ plans, enrollments }) => {
        this.plans.set(plans);
        this.enrolledPlanIds.set(
          new Set(enrollments.filter(e => e.status === 'Active').map(e => e.planId))
        );
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(errorMessage(err, 'Could not load plans.'));   // "Can't reach the server" only when it truly can't
        this.loading.set(false);
      },
    });
  }

  protected enroll(plan: Plan) {
    this.enrollingId.set(plan.id);   // disables every Enroll button while the request is in flight (no double-clicks)

    this.enrollmentService.enroll(plan.id).subscribe({
      next: () => {
        // update() = change a signal based on its current value. a NEW Set, so Angular notices the change
        this.enrolledPlanIds.update(ids => new Set(ids).add(plan.id));
        this.enrollingId.set(null);
        this.snackBar.open(`Enrolled in ${plan.name}. Your first premium is due on the 1st of next month.`, 'OK', {
          duration: 5000,
        });
      },
      error: (err: unknown) => {
        this.enrollingId.set(null);
        // e.g. "You're already enrolled in this plan." straight from the API
        this.snackBar.open(errorMessage(err, 'Enrollment failed. Please try again.'), 'OK', { duration: 5000 });
      },
    });
  }
}
