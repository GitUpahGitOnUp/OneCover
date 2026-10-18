// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     REPOSITORY INTERFACE for payments. just Add for now, statements + status queries come on Day 5
// Layer:    Repositories (data access)
// Concepts: ABSTRACTION, YAGNI (only what enrolling needs today)
// Related:  PaymentRepository, EnrollmentService (adds the premium payment), Payment.PendingPremium (B1),
//           Day 5 payments service (statements, mark paid)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;

namespace OneCover.Api.Repositories;

/// <summary>Data access for payments. Services use this instead of AppDbContext directly.</summary>
public interface IPaymentRepository
{
    // no SaveChangesAsync here: the service saves once through another repository, and since every repository
    // in a request shares the SAME DbContext, that one save writes the payment too (one transaction)
    void Add(Payment payment);
}
