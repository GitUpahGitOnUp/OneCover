// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     a route guard: only lets users w/ a given role open a section of the app
// Layer:    core. used on routes in app.routes.ts: canActivate: [roleGuard('Admin')]
// Concepts: a FUNCTION THAT RETURNS A FUNCTION (a "factory"): roleGuard('Admin') builds the actual guard.
//           returns true (allow) or a UrlTree (redirect). this is UX, NOT security: the API's [Authorize]
//           is the real gatekeeper, the guard just keeps people out of pages they can't use.
//           checks the token's EXPIRY at navigation time, and can't loop: a wrong-role user is sent to their
//           own area (which their role passes), and anyone w/o a valid session goes to /login (unguarded)
// Related:  AuthService (hasValidSession, role, homeRoute), app.routes.ts, the API's [Authorize(Roles = ...)]
// ─────────────────────────────────────────────────────────────────────────────────────────
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { Role } from '../shared/models';

// a guard decides whether a route can be opened. roleGuard('Admin') = "only admins get in"
export const roleGuard = (role: Role): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.hasValidSession()) {
    return router.createUrlTree(['/login']);           // no token, or it expired -> login page
  }
  return auth.role() === role
    ? true                                             // right role -> let them in
    : router.createUrlTree([auth.homeRoute()]);        // wrong role -> their own home (always a valid area)
};
