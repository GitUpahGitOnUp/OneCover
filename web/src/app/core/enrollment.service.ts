// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the front end's gateway to the enrollments API (my coverage + enrolling)
// Layer:    core (one app-wide instance)
// Concepts: DEPENDENCY INJECTION, OBSERVABLES, GENERICS. no user id is ever sent: the API knows who you are
//           from the token the interceptor attaches (members only see their own data)
// Related:  shared/models.ts (Enrollment), features/member/plans.ts, the API's EnrollmentsController
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api';
import { Enrollment } from '../shared/models';

@Service()   // Angular v22 singleton service (one shared instance, app-wide)
export class EnrollmentService {
  private http = inject(HttpClient);

  /** The logged-in member's enrollments, newest first. */
  getMine() {
    return this.http.get<Enrollment[]>(`${API_URL}/enrollments`);
  }

  /** Enrolls the logged-in member in a plan. The API also creates their first Pending premium. */
  enroll(planId: number) {
    return this.http.post<Enrollment>(`${API_URL}/enrollments`, { planId });
  }
}
