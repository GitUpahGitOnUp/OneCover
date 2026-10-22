// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for AuthService: login saves the token, a failed login doesn't, an expired token is ignored
// Layer:    Tests (Vitest + Angular TestBed)
// Concepts: TestBed = Angular's DI container for tests: we choose the providers (like registering fakes in .NET).
//           provideHttpClientTesting + HttpTestingController = a FAKE backend: no request leaves the test, we answer
//           it ourselves w/ flush(). fakeJwt builds a token-shaped string so the role/expiry code runs for real
//           (the signature is fake, which is fine: only the API ever verifies signatures)
// Related:  core/auth.service.ts, shared/jwt.ts, the API's AuthController (POST /api/auth/login)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { API_URL } from './api';

// header.payload.signature, each part base64url-encoded JSON (the same format the API produces)
function fakeJwt(payload: Record<string, unknown>): string {
  const encode = (part: unknown) =>
    btoa(JSON.stringify(part)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.fake-signature`;
}

const nowInSeconds = () => Math.floor(Date.now() / 1000);   // JWT exp is in SECONDS

describe('AuthService', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();   // each test starts logged out
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());   // fails the test if a request was sent that nobody answered

  it('saves the token and reads the role from it after a successful login', () => {
    const auth = TestBed.inject(AuthService);
    const token = fakeJwt({ role: 'Member', exp: nowInSeconds() + 3600 });

    auth.login({ email: 'member@onecover.dev', password: 'secret' }).subscribe();

    // the fake backend: check what was sent, then answer it
    const req = http.expectOne(`${API_URL}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({ token, role: 'Member' });

    expect(localStorage.getItem('token')).toBe(token);
    expect(auth.role()).toBe('Member');
    expect(auth.isLoggedIn()).toBe(true);
  });

  it('stays logged out when the API rejects the login', () => {
    const auth = TestBed.inject(AuthService);

    auth.login({ email: 'member@onecover.dev', password: 'wrong' }).subscribe({ error: () => {} });
    http.expectOne(`${API_URL}/auth/login`)
      .flush({ detail: 'Invalid email or password.' }, { status: 401, statusText: 'Unauthorized' });

    expect(localStorage.getItem('token')).toBeNull();
    expect(auth.isLoggedIn()).toBe(false);
  });

  it('ignores (and removes) an expired token left in localStorage', () => {
    localStorage.setItem('token', fakeJwt({ role: 'Member', exp: nowInSeconds() - 60 }));

    const auth = TestBed.inject(AuthService);   // created AFTER the old token was planted

    expect(auth.isLoggedIn()).toBe(false);
    expect(localStorage.getItem('token')).toBeNull();
  });
});
