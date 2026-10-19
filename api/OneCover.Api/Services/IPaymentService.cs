// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     SERVICE INTERFACE for payments: a member's statement, the admin list, the admin status update
// Layer:    Services (business logic)
// Concepts: ABSTRACTION. "who" comes from ICurrentUser, never a parameter
// Related:  PaymentService, PaymentsController (member), AdminPaymentsController (admin)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Services;

/// <summary>Payment logic for members (statements) and admins (status updates).</summary>
public interface IPaymentService
{
    /// <summary>The logged-in member's payments (premiums + benefits), newest due date first.</summary>
    Task<IReadOnlyList<PaymentDto>> GetMineAsync();

    /// <summary>Admin: all payments, optionally filtered to one status.</summary>
    Task<IReadOnlyList<PaymentDto>> GetAllAsync(PaymentStatus? status);

    /// <summary>
    /// Admin: marks a Pending payment Paid or Failed.
    /// Throws NotFoundException if it doesn't exist, BusinessRuleException if it isn't Pending or the status isn't Paid/Failed.
    /// </summary>
    Task<PaymentDto> UpdateStatusAsync(int id, UpdatePaymentStatusRequest request);
}
