// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the sign-up page: a new member's details -> AuthService.register -> logged in, sent to /member
// Layer:    features/auth (a page component)
// Concepts: REACTIVE FORMS w/ validators that MIRROR the API's RegistrationRequest (max lengths, 8-char password,
//           4-digit SSN pattern), so most mistakes never leave the browser; the API still re-checks everything.
//           spread operator (...v) to copy the form values into the request. no role field: everyone is a Member
// Related:  core/auth.service.ts, shared/models.ts (RegistrationRequest), the API's POST /api/auth/register
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject, signal } from '@angular/core';
import { errorMessage } from '../../shared/problem-details';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <mat-card class="form-card">
      <mat-card-header><mat-card-title>Create an account</mat-card-title></mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field class="full-width">
            <mat-label>First name</mat-label>
            <input matInput formControlName="firstName" autocomplete="given-name" />
            <mat-error>First name is required.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Last name</mat-label>
            <input matInput formControlName="lastName" autocomplete="family-name" />
            <mat-error>Last name is required.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
            <mat-error>Enter a valid email.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Password</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="new-password" />
            <mat-hint>At least 8 characters</mat-hint>
            <mat-error>Password must be at least 8 characters.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Hire date</mat-label>
            <!-- type="date" gives a "yyyy-MM-dd" string, exactly what the API's DateOnly expects -->
            <input matInput type="date" formControlName="hireDate" />
            <mat-error>Hire date is required.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Annual salary</mat-label>
            <input matInput type="number" formControlName="annualSalary" min="0" />
            <mat-error>Enter your annual salary.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Hours worked (last 12 months)</mat-label>
            <input matInput type="number" formControlName="hoursWorkedLast12Months" min="0" max="8760" />
            <mat-error>Enter a number from 0 to 8760.</mat-error>
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Last 4 of SSN</mat-label>
            <input matInput formControlName="ssnLast4" maxlength="4" inputmode="numeric" />
            <mat-hint>We only ever store the last 4 digits</mat-hint>
            <mat-error>Enter exactly 4 digits.</mat-error>
          </mat-form-field>

          @if (error()) { <p class="error" role="alert">{{ error() }}</p> }

          <button mat-flat-button class="full-width" type="submit" [disabled]="loading()">
            {{ loading() ? 'Creating account…' : 'Create account' }}
          </button>
        </form>
      </mat-card-content>
      <mat-card-actions>
        <a mat-button routerLink="/login">Already have an account? Log in</a>
      </mat-card-actions>
    </mat-card>
  `,
})
export class RegisterPage {
  private auth = inject(AuthService);
  private router = inject(Router);
  private fb = inject(NonNullableFormBuilder);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  // same rules as the API's RegistrationRequest, so most mistakes are caught before the request is sent
  protected readonly form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    hireDate: ['', Validators.required],
    annualSalary: [null as number | null, [Validators.required, Validators.min(0), Validators.max(10_000_000)]],
    hoursWorkedLast12Months: [null as number | null, [Validators.required, Validators.min(0), Validators.max(8760)]],
    ssnLast4: ['', [Validators.required, Validators.pattern(/^\d{4}$/)]],
  });

  protected submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    const v = this.form.getRawValue();
    this.auth
      .register({
        ...v,                                                   // copy every field...
        annualSalary: v.annualSalary ?? 0,                      // ...then fix the two number types
        hoursWorkedLast12Months: v.hoursWorkedLast12Months ?? 0, // (validation already guarantees they're filled)
      })
      .subscribe({
        next: () => this.router.navigateByUrl(this.auth.homeRoute()),
        error: (err: unknown) => {
          this.error.set(errorMessage(err, 'Registration failed. Please try again.'));   // also shows the first field error from a 400
          this.loading.set(false);
        },
      });
  }
}
