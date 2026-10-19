// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     EF Core implementation of IClaimRepository
// Layer:    Repositories (data access)
// Concepts: EAGER LOADING across several levels: Include + ThenInclude (claim -> enrollment -> plan,
//           claim -> history -> user). one shared query-builder method (DRY), AsNoTracking for read-only lists,
//           server-side PAGING (Skip/Take), GroupBy for counts, tuples for returning two values
// Related:  IClaimRepository, AppDbContext.Claims, BenefitClaim, ClaimMappings (needs all of this loaded)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using OneCover.Api.Data;
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Repositories;

public class ClaimRepository(AppDbContext db) : IClaimRepository
{
    // the base query every read shares, so the Includes are written once.
    //   .Include(c => c.User)                                         -> who filed it (MemberName in the DTO)
    //   .Include(c => c.Enrollment).ThenInclude(e => e.Plan)          -> claim's enrollment, then THAT enrollment's plan
    //   .Include(c => c.History).ThenInclude(h => h.ChangedByUser)    -> every history row, then who made each change
    // IQueryable = a query that's described but not run yet (deferred execution), so callers can keep adding to it
    private IQueryable<BenefitClaim> ClaimsWithDetails() => db.Claims
        .Include(c => c.User)
        .Include(c => c.Enrollment).ThenInclude(e => e.Plan)
        .Include(c => c.History).ThenInclude(h => h.ChangedByUser);

    public async Task<IReadOnlyList<BenefitClaim>> GetByUserAsync(int userId) => await ClaimsWithDetails()
        .AsNoTracking()
        .Where(c => c.UserId == userId)
        .OrderByDescending(c => c.SubmittedAt)
        .ToListAsync();

    // TRACKED on purpose: the admin status update loads the claim here, calls MoveTo, then saves,
    // and EF's change tracker turns that into an UPDATE + an INSERT for the new history row
    public Task<BenefitClaim?> GetByIdAsync(int id) => ClaimsWithDetails()
        .FirstOrDefaultAsync(c => c.Id == id);

    // PAGING IN SQL: only one page of rows ever leaves the database.
    //   1. build the filtered query (optionally one status)
    //   2. CountAsync on it -> the total, for "page 2 of 5"
    //   3. Skip((page - 1) * pageSize).Take(pageSize) -> just this page. page 3 of 20 = skip 40, take 20.
    //      an OrderBy is required first, or "page 3" could contain different rows each time
    public async Task<(IReadOnlyList<BenefitClaim> Items, int TotalCount)> GetPageAsync(
        ClaimStatus? status, int page, int pageSize)
    {
        var query = ClaimsWithDetails().AsNoTracking();

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);   // only add the filter when one was asked for
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.SubmittedAt)
            .ThenBy(c => c.Id)                          // tiebreaker so paging is stable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);   // a TUPLE: two values returned together w/o making a class for it
    }

    // LINQ GroupBy -> SQL GROUP BY: SELECT Status, COUNT(*) FROM Claims GROUP BY Status.
    // ToDictionaryAsync turns the rows into a lookup: { Submitted: 3, InReview: 1, ... }
    public async Task<IReadOnlyDictionary<ClaimStatus, int>> CountByStatusAsync() => await db.Claims
        .GroupBy(c => c.Status)
        .Select(g => new { Status = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.Status, x => x.Count);

    public void Add(BenefitClaim claim) => db.Claims.Add(claim);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
