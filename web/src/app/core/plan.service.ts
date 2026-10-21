// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the front end's gateway to the plans API
// Layer:    core (one app-wide instance). pages call this, never HttpClient directly
// Concepts: DEPENDENCY INJECTION (inject(HttpClient)), OBSERVABLES (each method returns one; the caller subscribes),
//           GENERICS (get<Plan[]> tells TypeScript what shape comes back), single responsibility (HTTP only, no UI)
// Related:  shared/models.ts (Plan), features/member/plans.ts, the API's PlansController (GET /api/plans).
//           admin methods (create, update, retire) get added on Day 8
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api';
import { Plan } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class PlanService {
  private http = inject(HttpClient);

  /** Active plans, sorted by name (public: no login needed). */
  getActive() {
    return this.http.get<Plan[]>(`${API_URL}/plans`);
  }
}
