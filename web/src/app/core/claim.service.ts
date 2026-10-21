// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the front end's gateway to the member claims API (list mine, get one, submit)
// Layer:    core (one app-wide instance)
// Concepts: DEPENDENCY INJECTION, OBSERVABLES, GENERICS. the request has no amounts and no user id:
//           the API calculates the benefit and takes "who" from the token
// Related:  shared/models.ts (Claim, SubmitClaimRequest), file-claim.ts, the claims list/detail pages (chunk 6),
//           the API's ClaimsController
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api';
import { Claim, SubmitClaimRequest } from '../shared/models';

@Service()   // Angular v22 singleton service (one shared instance, app-wide)
export class ClaimService {
  private http = inject(HttpClient);

  /** The logged-in member's claims, newest first. */
  getMine() {
    return this.http.get<Claim[]>(`${API_URL}/claims`);
  }

  /** One claim (members: only their own). */
  getById(id: number) {
    return this.http.get<Claim>(`${API_URL}/claims/${id}`);
  }

  /** Files a new claim. The response includes the benefit the API calculated. */
  submit(request: SubmitClaimRequest) {
    return this.http.post<Claim>(`${API_URL}/claims`, request);
  }
}
