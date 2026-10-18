// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     REPOSITORY INTERFACE for claims
// Layer:    Repositories (data access)
// Concepts: ABSTRACTION, single responsibility. every read returns claims w/ everything ToDto() needs loaded
// Related:  ClaimRepository (implementation), ClaimService (caller), Program.cs (AddScoped)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;

namespace OneCover.Api.Repositories;

/// <summary>Data access for claims. Services use this instead of AppDbContext directly.</summary>
public interface IClaimRepository
{
    /// <summary>A member's claims, newest first, with plan + history loaded (read-only).</summary>
    Task<IReadOnlyList<BenefitClaim>> GetByUserAsync(int userId);

    /// <summary>One claim with plan + history loaded, or null. Tracked, so Day 5 status changes can be saved.</summary>
    Task<BenefitClaim?> GetByIdAsync(int id);

    void Add(BenefitClaim claim);

    Task SaveChangesAsync();
}
