// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the admin area's child routes (everything under /admin)
// Layer:    features/admin. loaded lazily by app.routes.ts, already behind roleGuard('Admin')
// Concepts: CHILD ROUTES + LAZY LOADING: admin code is never even downloaded for a member
// Related:  app.routes.ts, the admin pages (dashboard, claims queue, claim review, payments, plans)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./dashboard').then(m => m.DashboardPage) },
  { path: 'claims', loadComponent: () => import('./claims-queue').then(m => m.ClaimsQueuePage) },
  { path: 'claims/:id', loadComponent: () => import('./claim-review').then(m => m.ClaimReviewPage) },
  { path: 'payments', loadComponent: () => import('./payments').then(m => m.AdminPaymentsPage) },
  { path: 'plans', loadComponent: () => import('./plans-admin').then(m => m.PlansAdminPage) },
];
