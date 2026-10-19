// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     EF Core implementation of IPaymentRepository
// Layer:    Repositories (data access)
// Concepts: EAGER LOADING through OPTIONAL navigations (Include on a nullable navigation just leaves it null when
//           there's no related row), one shared query-builder (DRY), AsNoTracking for read-only lists
// Related:  IPaymentRepository, AppDbContext.Payments, Payment, PaymentMappings, PaymentService
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using OneCover.Api.Data;
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Repositories;

public class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    public void Add(Payment payment) => db.Payments.Add(payment);

    // everything ToDto() reads: the member, plus the plan reached through EITHER the enrollment (premiums)
    // OR the claim's enrollment (benefits). the ! tells the compiler "I know this navigation can be null,
    // EF handles that". a premium just gets a null Claim, a benefit a null Enrollment
    private IQueryable<Payment> PaymentsWithDetails() => db.Payments
        .Include(p => p.User)
        .Include(p => p.Enrollment!).ThenInclude(e => e.Plan)
        .Include(p => p.Claim!).ThenInclude(c => c.Enrollment).ThenInclude(e => e.Plan);

    public async Task<IReadOnlyList<Payment>> GetByUserAsync(int userId) => await PaymentsWithDetails()
        .AsNoTracking()
        .Where(p => p.UserId == userId)
        .OrderByDescending(p => p.DueDate).ThenByDescending(p => p.Id)
        .ToListAsync();

    public async Task<IReadOnlyList<Payment>> GetAllAsync(PaymentStatus? status)
    {
        var query = PaymentsWithDetails().AsNoTracking();
        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        return await query.OrderByDescending(p => p.DueDate).ThenByDescending(p => p.Id).ToListAsync();
    }

    // tracked: the admin status update calls MarkPaid/MarkFailed on this object, then saves
    public Task<Payment?> GetByIdAsync(int id) => PaymentsWithDetails().FirstOrDefaultAsync(p => p.Id == id);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
