// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the front end's gateway to the member's payments (their statement)
// Layer:    core (one app-wide instance)
// Concepts: DEPENDENCY INJECTION, OBSERVABLES, GENERICS. "whose" payments comes from the token, not the URL.
//           admin methods (list all, mark paid/failed) get added on Day 8
// Related:  shared/models.ts (Payment), features/member/statements.ts, the API's PaymentsController
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api';
import { Payment } from '../shared/models';

@Service()   // Angular v22 singleton service (one shared instance, app-wide)
export class PaymentService {
  private http = inject(HttpClient);

  /** The logged-in member's premium + benefit payments, newest due date first. */
  getMine() {
    return this.http.get<Payment[]>(`${API_URL}/payments`);
  }
}
