// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for AuthService: login, a rejected login, restoring a saved token, and mid-session expiry
// Layer:    Tests (Vitest + Angular TestBed)
// Concepts: TestBed = Angular's DI container for tests: we choose the providers (like registering fakes in .NET).
//           provideHttpClientTesting + HttpTestingController = a FAKE backend: no request leaves the test, we answer
//           it ourselves w/ flush(). tokens use the API's real (long) role claim. FAKE TIMERS (vi.setSystemTime)
//           move the clock forward, so "the token expired while the tab was open" is testable in milliseconds.
//           each test is designed to FAIL if the behavior it names breaks (e.g. the role must come from the token)
// Related:  core/auth.service.ts, shared/jwt.ts, testing/fake-jwt.ts, role.guard.ts (calls hasValidSession)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { API_URL } from './api';
import { apiToken, fakeJwt, nowInSeconds } from '../../testing/fake-jwt';

describe('AuthService', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();   // each test starts logged out
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();          // fails the test if a request was sent that nobody answered
    vi.useRealTimers();     // undo any fake clock from the expiry test
  });

  describe('login', () => {
    it('saves the token and takes the role FROM THE TOKEN, not the response body', () => {
      const auth = TestBed.inject(AuthService);
      const token = apiToken('Admin');   // the long role claim, like the real API

      auth.login({ email: 'admin@onecover.dev', password: 'secret' }).subscribe();

      // the fake backend: check what was sent, then answer it
      const req = http.expectOne(`${API_URL}/auth/login`);
      expect(req.request.method).toBe('POST');
      // the body says Member, the token says Admin: the token must win (it's the signed source of truth)
      req.flush({ token, role: 'Member' });

      expect(localStorage.getItem('token')).toBe(token);
      expect(auth.role()).toBe('Admin');
      expect(auth.isLoggedIn()).toBe(true);
    });

    it('passes a rejected login on to the caller as an error and stays logged out', () => {
      const auth = TestBed.inject(AuthService);
      let error: HttpErrorResponse | undefined;

      auth.login({ email: 'member@onecover.dev', password: 'wrong' }).subscribe({
        next: () => expect.fail('a rejected login must not succeed'),
        error: (err: HttpErrorResponse) => (error = err),
      });
      http.expectOne(`${API_URL}/auth/login`)
        .flush({ detail: 'Invalid email or password.' }, { status: 401, statusText: 'Unauthorized' });

      // the login page needs this error to show "Invalid email or password."
      expect(error?.status).toBe(401);
      expect(localStorage.getItem('token')).toBeNull();
      expect(auth.isLoggedIn()).toBe(false);
    });
  });

  describe('restoring a saved token on startup', () => {
    it('restores a valid token (with the short "role" claim too)', () => {
      localStorage.setItem('token', fakeJwt({ role: 'Member', exp: nowInSeconds() + 3600 }));

      const auth = TestBed.inject(AuthService);   // created AFTER the token was saved

      expect(auth.role()).toBe('Member');
    });

    it.each([
      ['expired', fakeJwt({ role: 'Member', exp: nowInSeconds() - 60 })],
      ['an unknown role', fakeJwt({ role: 'member', exp: nowInSeconds() + 3600 })],   // wrong case: not a real role
      ['no role at all', fakeJwt({ exp: nowInSeconds() + 3600 })],
      ['malformed', 'not-a-jwt'],
    ])('ignores and removes a token that is %s', (_label, token) => {
      localStorage.setItem('token', token);

      const auth = TestBed.inject(AuthService);

      expect(auth.isLoggedIn()).toBe(false);
      expect(localStorage.getItem('token')).toBeNull();
    });
  });

  describe('hasValidSession', () => {
    it('logs out when the token expires while the tab is open', () => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date('2026-10-22T12:00:00Z'));
      localStorage.setItem('token', apiToken('Member', 60));   // valid for one more minute
      const auth = TestBed.inject(AuthService);
      expect(auth.hasValidSession()).toBe(true);

      vi.setSystemTime(new Date('2026-10-22T12:02:00Z'));      // two minutes later

      expect(auth.hasValidSession()).toBe(false);              // what role.guard checks on navigation
      expect(auth.isLoggedIn()).toBe(false);
      expect(localStorage.getItem('token')).toBeNull();        // the session was actually cleared
    });
  });
});
