// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the admin side of claims: the paged queue, moving a claim through the workflow, dashboard counts
// Layer:    core (one app-wide instance). admin pages call this, never HttpClient directly
// Concepts: QUERY PARAMETERS: HttpClient turns { page: 2, pageSize: 20 } into ?page=2&pageSize=20.
//           GENERICS: get<PagedResult<Claim>> = "one page of claims". OBSERVABLES (the caller subscribes).
//           one claim for review = ClaimService.getById (the API lets admins read any claim), so no copy here (DRY)
// Related:  shared/models.ts (PagedResult, UpdateClaimStatusRequest, Dashboard), the API's AdminClaimsController
//           + AdminDashboardController, features/admin (claims queue, claim review, dashboard)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api';
import { Claim, ClaimStatus, Dashboard, PagedResult, UpdateClaimStatusRequest } from '../shared/models';

@Service()
export class AdminClaimService {
  private http = inject(HttpClient);
  private readonly url = `${API_URL}/admin/claims`;

  /** One page of the claims queue, newest first. page is 1-based; status null = every status. */
  getPage(page: number, pageSize: number, status: ClaimStatus | null) {
    // Record<string, string | number> = an object w/ string keys. status is only added when there IS one,
    // so "all statuses" sends no status param at all (the API treats a missing one as "no filter")
    const params: Record<string, string | number> = { page, pageSize };
    if (status) {
      params['status'] = status;
    }
    return this.http.get<PagedResult<Claim>>(this.url, { params });
  }

  /** Move a claim to a new status. A move the workflow doesn't allow comes back as a 400 with the reason. */
  updateStatus(id: number, request: UpdateClaimStatusRequest) {
    return this.http.put<Claim>(`${this.url}/${id}/status`, request);
  }

  /** Claim counts by status for the dashboard cards. */
  getDashboard() {
    return this.http.get<Dashboard>(`${API_URL}/admin/dashboard`);
  }
}
