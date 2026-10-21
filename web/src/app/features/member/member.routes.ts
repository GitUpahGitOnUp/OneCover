// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the member area's child routes (everything under /member)
// Layer:    features/member. loaded lazily by app.routes.ts, already behind roleGuard('Member')
// Concepts: CHILD ROUTES + LAZY LOADING: these paths are relative, so 'plans' here = /member/plans
// Related:  app.routes.ts, the member pages (plans, coverage, claims, statements, profile)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Routes } from '@angular/router';

export const MEMBER_ROUTES: Routes = [
  { path: 'plans', loadComponent: () => import('./plans').then(m => m.PlansPage) },
  { path: '', pathMatch: 'full', redirectTo: 'plans' },
];
