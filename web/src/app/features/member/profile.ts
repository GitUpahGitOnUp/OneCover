// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the member's profile: name, email, hire date, masked SSN, and an FMLA eligibility message
// Layer:    features/member (a page component)
// Concepts: a custom PIPE in use (| maskSsn), SIGNALS w/ a nullable value (profile is null until it loads),
//           @if to show different messages by eligibility. the FMLA RULE lives in the API (EligibilityService);
//           this page only DISPLAYS the result, so the rule exists in exactly one place
// Related:  core/profile.service.ts, shared/mask-ssn.pipe.ts, the API's GET /api/me + EligibilityService
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { ProfileService } from '../../core/profile.service';
import { MaskSsnPipe } from '../../shared/mask-ssn.pipe';
import { Profile } from '../../shared/models';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-profile',
  imports: [DatePipe, MatCardModule, MaskSsnPipe],
  template: `
    <h1>My profile</h1>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (profile(); as p) {
      <!-- "as p" = give the loaded profile a short name for the rest of this block -->
      <mat-card>
        <mat-card-header>
          <mat-card-title>{{ p.firstName }} {{ p.lastName }}</mat-card-title>
          <mat-card-subtitle>{{ p.email }}</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <dl class="details">
            <dt>Hire date</dt>
            <dd>{{ p.hireDate | date: 'mediumDate' : 'UTC' }}</dd>
            <dt>SSN</dt>
            <dd>{{ p.ssnLast4 | maskSsn }}</dd>
            <dt>Hours worked (last 12 months)</dt>
            <dd>{{ p.hoursWorkedLast12Months }}</dd>
          </dl>

          <h2>FMLA leave</h2>
          @if (p.isFmlaEligible) {
            <p class="eligible">
              ✓ You're eligible for FMLA: up to 12 weeks of job-protected leave for a qualifying reason.
            </p>
          } @else {
            <p>
              You're not eligible for FMLA yet. It requires at least 12 months with the company and
              1,250 hours worked in the last 12 months.
            </p>
          }
        </mat-card-content>
      </mat-card>
    } @else {
      <p>Loading your profile…</p>
    }
  `,
  styles: `
    .details { display: grid; grid-template-columns: max-content 1fr; gap: 8px 24px; }
    .details dt { font-weight: 500; }
    .details dd { margin: 0; }
    .eligible { color: var(--mat-sys-primary); font-weight: 500; }
  `,
})
export class ProfilePage {
  private profileService = inject(ProfileService);

  protected readonly profile = signal<Profile | null>(null);   // null = still loading
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.profileService.getMine().subscribe({
      next: p => this.profile.set(p),
      error: (err: unknown) => this.error.set(errorMessage(err, 'Could not load your profile.')),
    });
  }
}
