// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the front end's gateway to GET /api/me (the logged-in user's profile + FMLA eligibility)
// Layer:    core (one app-wide instance)
// Concepts: DEPENDENCY INJECTION, OBSERVABLES, GENERICS. no id in the URL: "me" always comes from the token
// Related:  shared/models.ts (Profile), features/member/profile.ts, the API's MeController
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api';
import { Profile } from '../shared/models';

@Service()   // Angular v22 singleton service (one shared instance, app-wide)
export class ProfileService {
  private http = inject(HttpClient);

  /** The logged-in user's profile, including FMLA eligibility. */
  getMine() {
    return this.http.get<Profile>(`${API_URL}/me`);
  }
}
