// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the admin side of payments: every member's payments (optionally filtered), and marking one Paid/Failed
// Layer:    core (one app-wide instance)
// Concepts: same pattern as AdminClaimService: optional QUERY PARAMETER, OBSERVABLES. which moves are allowed
//           (only from Pending, no future paid dates) is the API's job (Payment.MarkPaid / MarkFailed)
// Related:  shared/models.ts (Payment, UpdatePaymentStatusRequest), the API's AdminPaymentsController,
//           features/admin/payments.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api';
import { Payment, PaymentStatus, UpdatePaymentStatusRequest } from '../shared/models';

@Service()
export class AdminPaymentService {
  private http = inject(HttpClient);
  private readonly url = `${API_URL}/admin/payments`;

  /** All members' payments, newest due date first. status null = every status. */
  getAll(status: PaymentStatus | null) {
    const params: Record<string, string> = status ? { status } : {};
    return this.http.get<Payment[]>(this.url, { params });
  }

  /** Mark a payment Paid (paidOn null = today) or Failed. Only Pending payments can change (400 otherwise). */
  updateStatus(id: number, request: UpdatePaymentStatusRequest) {
    return this.http.put<Payment>(`${this.url}/${id}/status`, request);
  }
}
