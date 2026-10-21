// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the app's login state + the login / register / logout actions
// Layer:    core (one app-wide instance)
// Concepts: DEPENDENCY INJECTION w/ inject() (like constructor injection in .NET), SIGNALS (reactive state:
//           anything reading token()/role() re-renders when they change), computed() for DERIVED state,
//           OBSERVABLES (HttpClient returns one; nothing is sent until someone subscribes), RxJS tap() for side
//           effects, ENCAPSULATION (saveSession/clearSession are private: only this class writes the session).
//           the JWT is the SINGLE SOURCE OF TRUTH: the role is read from the token, not stored separately,
//           and an expired or malformed token is treated as logged out
// Related:  shared/jwt.ts (reads exp + role), auth.interceptor.ts, role.guard.ts, login + register pages,
//           api/.../AuthController (POST /auth/login, /auth/register)
// Note:     localStorage is fine for a training project. production apps often prefer httpOnly cookies
//           (JavaScript can't read them, which protects the token from XSS). a good interview point
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Service, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { API_URL } from './api';
import { AuthResponse, LoginRequest, RegistrationRequest, Role } from '../shared/models';
import { isTokenExpired, roleFromToken } from '../shared/jwt';

const TOKEN_KEY = 'token';
const OLD_ROLE_KEY = 'role';   // earlier versions stored the role separately; cleaned up wherever the session is cleared

// @Service() = Angular v22's way to declare a singleton service (one shared instance, app-wide)
@Service()
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // restored from localStorage on startup, but ONLY if it's still a usable token (not expired, has a real role)
  readonly token = signal<string | null>(restoreToken());

  // derived from the token: re-computed automatically whenever token changes
  readonly role = computed<Role | null>(() => {
    const token = this.token();
    return token ? roleFromToken(token) : null;
  });
  readonly isLoggedIn = computed(() => this.role() !== null);

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

  /**
   * True if there's a token that hasn't expired yet. Checked at navigation time (by the guard), because a token
   * that was fine when the page loaded can expire while the tab sits open. Clears an expired session.
   */
  hasValidSession(): boolean {
    const token = this.token();
    if (token && isTokenExpired(token)) {
      this.clearSession();
      return false;
    }
    return this.isLoggedIn();
  }

  // JWTs are stateless: "logging out" just means throwing the token away
  logout() {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  // where each role lands after logging in. no valid role = back to login (never a guarded page -> no redirect loop)
  homeRoute(): string {
    switch (this.role()) {
      case 'Admin': return '/admin';
      case 'Member': return '/member';
      default: return '/login';
    }
  }

  private saveSession(res: AuthResponse) {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.removeItem(OLD_ROLE_KEY);
    this.token.set(res.token);   // role + isLoggedIn update automatically (computed)
  }

  private clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(OLD_ROLE_KEY);
    this.token.set(null);
  }
}

// reads the saved token, but throws it away if it's expired or doesn't carry a valid role
function restoreToken(): string | null {
  const token = localStorage.getItem(TOKEN_KEY);
  if (!token || isTokenExpired(token) || roleFromToken(token) === null) {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(OLD_ROLE_KEY);
    return null;
  }
  return token;
}
