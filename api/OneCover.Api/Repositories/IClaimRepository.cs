// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     REPOSITORY INTERFACE for claims
// Layer:    Repositories (data access)
// Concepts: ABSTRACTION, single responsibility. every read returns claims w/ everything ToDto() needs loaded.
//           tuple return type for paging (items + total in one call)
// Related:  ClaimRepository (implementation), ClaimService (caller), Program.cs (AddScoped)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Repositories;

/// <summary>Data access for claims. Services use this instead of AppDbContext directly.</summary>
public interface IClaimRepository
{
    /// <summary>A member's claims, newest first, with plan + history loaded (read-only).</summary>
    Task<IReadOnlyList<BenefitClaim>> GetByUserAsync(int userId);

    /// <summary>One claim with member, plan + history loaded, or null. Tracked, so status changes can be saved.</summary>
    Task<BenefitClaim?> GetByIdAsync(int id);

    /// <summary>One page of claims (newest first), optionally filtered to one status, plus the total matching count.</summary>
    Task<(IReadOnlyList<BenefitClaim> Items, int TotalCount)> GetPageAsync(ClaimStatus? status, int page, int pageSize);

    /// <summary>How many claims are in each status. Statuses with no claims are simply missing from the result.</summary>
    Task<IReadOnlyDictionary<ClaimStatus, int>> CountByStatusAsync();

    void Add(BenefitClaim claim);

    Task SaveChangesAsync();
}
