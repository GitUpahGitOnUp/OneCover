// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     adds "Authorization: Bearer <token>" to every API request, and logs out when the token stops working
// Layer:    core. registered in app.config.ts w/ withInterceptors([authInterceptor])
// Concepts: functional INTERCEPTOR (a function, not a class), like MIDDLEWARE in the API's pipeline.
//           immutability (clone() instead of editing the request), RxJS catchError + throwError,
//           inject() inside a function (DI works here b/c Angular runs it in an injection context)
// Related:  AuthService (token + logout), app.config.ts, the API's JWT middleware (checks the header this adds)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

// runs on *EVERY* HTTP request the app makes, like middleware on the API side
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  // requests are immutable, so clone() makes a copy with the Authorization header added
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      // a 401 while we HAD a token = it expired or is invalid -> log out.
      // (a 401 with no token is just a wrong password on the login form, so leave that to the page)
      if (err.status === 401 && token) {
        auth.logout();
      }
      return throwError(() => err);   // pass the error on so the page can still show a message
    })
  );
};
