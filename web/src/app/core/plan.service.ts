// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the front end's gateway to the plans API
// Layer:    core (one app-wide instance). pages call this, never HttpClient directly
// Concepts: DEPENDENCY INJECTION (inject(HttpClient)), OBSERVABLES (each method returns one; the caller subscribes),
//           GENERICS (get<Plan[]> tells TypeScript what shape comes back), single responsibility (HTTP only, no UI)
// Related:  shared/models.ts (Plan), features/member/plans.ts, the API's PlansController (GET /api/plans).
//           admin methods (create, update, retire) get added on Day 8
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api';
import { Plan, SavePlanRequest } from '../shared/models';

@Service()   // Angular v22 singleton service (one shared instance, app-wide)
export class PlanService {
  private http = inject(HttpClient);

  /** Active plans, sorted by name (public: no login needed). */
  getActive() {
    return this.http.get<Plan[]>(`${API_URL}/plans`);
  }

  /** Admin: create a plan. The API answers 201 Created with the saved plan (new id included). */
  create(request: SavePlanRequest) {
    return this.http.post<Plan>(`${API_URL}/plans`, request);
  }

  /** Admin: replace a plan's details. */
  update(id: number, request: SavePlanRequest) {
    return this.http.put<Plan>(`${API_URL}/plans/${id}`, request);
  }

  /** Admin: retire a plan (a SOFT delete: it stays in the DB, hidden from members). 204 = no body, so void. */
  retire(id: number) {
    return this.http.delete<void>(`${API_URL}/plans/${id}`);
  }
}
