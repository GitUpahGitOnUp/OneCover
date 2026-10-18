// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     EF Core implementation of IClaimRepository
// Layer:    Repositories (data access)
// Concepts: EAGER LOADING across several levels: Include + ThenInclude (claim -> enrollment -> plan,
//           claim -> history -> user). one shared query-builder method (DRY), AsNoTracking for read-only lists
// Related:  IClaimRepository, AppDbContext.Claims, BenefitClaim, ClaimMappings (needs all of this loaded)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using OneCover.Api.Data;
using OneCover.Api.Models;

namespace OneCover.Api.Repositories;

public class ClaimRepository(AppDbContext db) : IClaimRepository
{
    // the base query both reads share, so the Includes are written once.
    //   .Include(c => c.Enrollment).ThenInclude(e => e.Plan)          -> claim's enrollment, then THAT enrollment's plan
    //   .Include(c => c.History).ThenInclude(h => h.ChangedByUser)    -> every history row, then who made each change
    // IQueryable = a query that's described but not run yet (deferred execution), so callers can keep adding to it
    private IQueryable<BenefitClaim> ClaimsWithDetails() => db.Claims
        .Include(c => c.Enrollment).ThenInclude(e => e.Plan)
        .Include(c => c.History).ThenInclude(h => h.ChangedByUser);

    public async Task<IReadOnlyList<BenefitClaim>> GetByUserAsync(int userId) => await ClaimsWithDetails()
        .AsNoTracking()
        .Where(c => c.UserId == userId)
        .OrderByDescending(c => c.SubmittedAt)
        .ToListAsync();

    public Task<BenefitClaim?> GetByIdAsync(int id) => ClaimsWithDetails()
        .FirstOrDefaultAsync(c => c.Id == id);

    public void Add(BenefitClaim claim) => db.Claims.Add(claim);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
