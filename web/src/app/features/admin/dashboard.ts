// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     TEMPORARY placeholder for the admin dashboard (/admin). Day 8 replaces it w/ the claim-count cards
//           (from GET /api/admin/dashboard)
// Layer:    features/admin (a page component)
// Concepts: a minimal standalone component, here so the admin route + login redirect have somewhere to land
// Related:  admin.routes.ts (loads it lazily), AuthService.homeRoute() (admins land here after login)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  template: `<h1>Admin dashboard</h1><p>Coming on Day 8.</p>`,
})
export class DashboardPage {}
