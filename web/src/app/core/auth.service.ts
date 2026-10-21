// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the app's login state + the login / register / logout actions
// Layer:    core (one app-wide instance)
// Concepts: DEPENDENCY INJECTION w/ inject() (like constructor injection in .NET), SIGNALS (reactive state:
//           anything reading token()/role() re-renders when they change), computed() for derived state,
//           OBSERVABLES (HttpClient returns one; nothing is sent until someone subscribes), RxJS tap() for side
//           effects, ENCAPSULATION (saveSession is private: only this class writes the session)
// Related:  auth.interceptor.ts (reads token), role.guard.ts (reads role), login + register pages,
//           api/.../AuthController (POST /auth/login, /auth/register)
// Note:     localStorage is fine for a training project. production apps often prefer httpOnly cookies
//           (JavaScript can't read them, which protects the token from XSS). a good interview point
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { API_URL } from './api';
import { AuthResponse, LoginRequest, RegistrationRequest, Role } from '../shared/models';

// one shared instance for the whole app (providedIn: 'root'): every component sees the same login state
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // signals = reactive values. anything reading them (the toolbar, the guards) updates automatically when they change.
  // they start from localStorage, so a page refresh keeps you logged in
  readonly token = signal<string | null>(localStorage.getItem('token'));
  readonly role = signal<Role | null>(localStorage.getItem('role') as Role | null);
  readonly isLoggedIn = computed(() => this.token() !== null);   // derived: recalculates when token changes

  // returns an Observable: nothing is sent until a component subscribes. tap() saves the session on success
  login(request: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${API_URL}/auth/login`, request)
      .pipe(tap(res => this.saveSession(res)));
  }

  register(request: RegistrationRequest) {
    return this.http
      .post<AuthResponse>(`${API_URL}/auth/register`, request)
      .pipe(tap(res => this.saveSession(res)));
  }

  // JWTs are stateless: "logging out" just means throwing the token away
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    this.token.set(null);
    this.role.set(null);
    this.router.navigate(['/login']);
  }

  // where each role lands after logging in
  homeRoute(): string {
    return this.role() === 'Admin' ? '/admin' : '/member';
  }

  private saveSession(res: AuthResponse) {
    localStorage.setItem('token', res.token);
    localStorage.setItem('role', res.role);
    this.token.set(res.token);
    this.role.set(res.role);
  }
}
