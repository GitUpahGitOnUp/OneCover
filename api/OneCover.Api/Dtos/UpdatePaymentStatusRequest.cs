// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for an admin marking a payment Paid or Failed: the JSON body of PUT /api/admin/payments/{id}/status
// Layer:    Dtos, the API boundary
// Concepts: validation attributes. which moves are allowed (only from Pending) is enforced by the Payment entity's
//           MarkPaid / MarkFailed, not here
// Related:  AdminPaymentsController, PaymentService.UpdateStatusAsync, Payment.MarkPaid / MarkFailed
// ─────────────────────────────────────────────────────────────────────────────────────────
using System.ComponentModel.DataAnnotations;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

/// <summary>Mark a payment Paid (optionally on a given date, default today) or Failed.</summary>
public record UpdatePaymentStatusRequest
{
    // only Paid or Failed make sense here. the service rejects Pending w/ a clear 400
    [Required, EnumDataType(typeof(PaymentStatus))]
    public PaymentStatus? Status { get; init; }

    // optional: when the payment went through. if left out on a Paid update, today is used
    public DateOnly? PaidOn { get; init; }
}
