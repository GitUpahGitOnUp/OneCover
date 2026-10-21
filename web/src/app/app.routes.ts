// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the app's top-level URL map: which page/area opens for each URL
// Layer:    app root (the front-end version of the API's route table)
// Concepts: LAZY LOADING (loadComponent / loadChildren download a page's code only when it's visited),
//           route GUARDS (canActivate: [roleGuard('Member')]), redirects + a '**' catch-all for unknown URLs
// Related:  core/role.guard.ts, features/*/…routes.ts (each area's child routes), app.config.ts (provideRouter)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Routes } from '@angular/router';
import { roleGuard } from './core/role.guard';

export const routes: Routes = [
  // loadComponent / loadChildren = LAZY LOADING: the page's code is only downloaded when someone visits it
  { path: 'login', loadComponent: () => import('./features/auth/login').then(m => m.LoginPage) },
  { path: 'register', loadComponent: () => import('./features/auth/register').then(m => m.RegisterPage) },

  // each area is guarded by role, and loads its own child routes
  {
    path: 'member',
    canActivate: [roleGuard('Member')],
    loadChildren: () => import('./features/member/member.routes').then(m => m.MEMBER_ROUTES),
  },
  {
    path: 'admin',
    canActivate: [roleGuard('Admin')],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES),
  },

  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' },   // any unknown URL -> login
];

