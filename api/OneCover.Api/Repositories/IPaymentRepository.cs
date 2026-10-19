// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     REPOSITORY INTERFACE for payments
// Layer:    Repositories (data access)
// Concepts: ABSTRACTION, single responsibility. reads return payments w/ everything ToDto() needs loaded
// Related:  PaymentRepository, PaymentService (statements + admin status), EnrollmentService (adds premiums),
//           ClaimService (adds benefits on approval), Program.cs (AddScoped)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Repositories;

/// <summary>Data access for payments. Services use this instead of AppDbContext directly.</summary>
public interface IPaymentRepository
{
    // no SaveChangesAsync needed by EnrollmentService/ClaimService: they save once through their own repository,
    // and since every repository in a request shares the SAME DbContext, that one save writes the payment too
    void Add(Payment payment);

    /// <summary>A member's payments (their statement), newest due date first, read-only.</summary>
    Task<IReadOnlyList<Payment>> GetByUserAsync(int userId);

    /// <summary>All payments for the admin page, optionally filtered to one status, read-only.</summary>
    Task<IReadOnlyList<Payment>> GetAllAsync(PaymentStatus? status);

    /// <summary>One payment, tracked so MarkPaid/MarkFailed can be saved. Null if not found.</summary>
    Task<Payment?> GetByIdAsync(int id);

    Task SaveChangesAsync();
}
