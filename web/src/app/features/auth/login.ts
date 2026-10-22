// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the login page: email + password form -> AuthService.login -> go to the user's home area
// Layer:    features/auth (a page component)
// Concepts: REACTIVE FORMS (fb.group + Validators = validate at the edge, before anything is sent),
//           SIGNALS for page state (loading, error), OBSERVABLES (subscribe() actually sends the request),
//           inline template w/ @if control flow, showing the API's ProblemDetails "detail" message on error
// Related:  core/auth.service.ts, app.routes.ts (/login), the API's POST /api/auth/login
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
import { Logo } from '../../shared/logo';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, Logo],
  template: `
    <div class="welcome">
      <app-logo [size]="56" />
      <h1>Welcome to One<strong>Cover</strong></h1>
      <p>Enroll in benefits, file leave claims and track them, all in one place.</p>
    </div>

    <mat-card class="form-card">
      <mat-card-header><mat-card-title>Log in</mat-card-title></mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field class="full-width">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
            @if (form.controls.email.hasError('required')) { <mat-error>Email is required.</mat-error> }
            @else if (form.controls.email.hasError('email')) { <mat-error>Enter a valid email.</mat-error> }
          </mat-form-field>

          <mat-form-field class="full-width">
            <mat-label>Password</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="current-password" />
            @if (form.controls.password.hasError('required')) { <mat-error>Password is required.</mat-error> }
          </mat-form-field>

          @if (error()) { <p class="error" role="alert">{{ error() }}</p> }

          <button mat-flat-button class="full-width" type="submit" [disabled]="loading()">
            {{ loading() ? 'Logging in…' : 'Log in' }}
          </button>
        </form>
      </mat-card-content>
      <mat-card-actions>
        <a mat-button routerLink="/register">Create an account</a>
      </mat-card-actions>
    </mat-card>
  `,
})
export class LoginPage {
  private auth = inject(AuthService);
  private router = inject(Router);
  private fb = inject(NonNullableFormBuilder);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  // reactive form: the validators run in the browser before anything is sent (validate at the edges)
  protected readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();   // shows the red errors on every field
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    // subscribe() is what actually sends the request (Observables are lazy)
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl(this.auth.homeRoute()),
      error: (err: unknown) => {
        // the API's ProblemDetails "detail", e.g. "Invalid email or password."
        this.error.set(errorMessage(err, 'Login failed. Please try again.'));   // the API's message, e.g. "Invalid email or password."
        this.loading.set(false);
      },
    });
  }
}
