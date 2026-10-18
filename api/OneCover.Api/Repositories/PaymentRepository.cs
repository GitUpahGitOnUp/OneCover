// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     EF Core implementation of IPaymentRepository
// Layer:    Repositories (data access)
// Concepts: implements an INTERFACE, DEPENDENCY INJECTION, change tracker (Add marks for insert)
// Related:  IPaymentRepository, AppDbContext.Payments, Payment, EnrollmentService
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Data;
using OneCover.Api.Models;

namespace OneCover.Api.Repositories;

public class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    public void Add(Payment payment) => db.Payments.Add(payment);
}
