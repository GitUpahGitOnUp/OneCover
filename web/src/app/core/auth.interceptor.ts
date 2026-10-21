// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     adds "Authorization: Bearer <token>" to requests going to OUR API, and logs out when the token stops working
// Layer:    core. registered in app.config.ts w/ withInterceptors([authInterceptor])
// Concepts: functional INTERCEPTOR (a function, not a class), like MIDDLEWARE in the API's pipeline.
//           immutability (clone() instead of editing the request), RxJS catchError + throwError,
//           inject() inside a function (DI works here b/c Angular runs it in an injection context).
//           SECURITY: the token is only ever sent to API_URL (never to a third-party host), and never on the
//           login/register calls themselves (a stale token there means nothing, and a wrong password shouldn't log you out)
// Related:  AuthService (token + logout), core/api.ts (API_URL), app.config.ts, the API's JWT middleware
// ─────────────────────────────────────────────────────────────────────────────────────────
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API_URL } from './api';
import { AuthService } from './auth.service';

// runs on *EVERY* HTTP request the app makes, like middleware on the API side
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  const isApiCall = req.url.startsWith(API_URL);              // only OUR API ever gets the token
  const isAuthCall = req.url.startsWith(`${API_URL}/auth/`);  // login/register don't need one
  const sendsToken = token !== null && isApiCall && !isAuthCall;

  // requests are immutable, so clone() makes a copy with the Authorization header added
  const outgoing = sendsToken
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(outgoing).pipe(
    catchError((err: HttpErrorResponse) => {
      // a 401 on a request that CARRIED our token = it expired or is invalid -> log out.
      // a 401 from login itself (wrong password) never carried a token, so it's left for the page to show
      if (err.status === 401 && sendsToken) {
        auth.logout();
      }
      return throwError(() => err);   // pass the error on so the page can still show a message
    })
  );
};
