// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     entity for one status change on a claim (one row in ClaimStatusHistories)
// Layer:    Models (domain)
// Concepts: IMMUTABILITY w/ init: every fact can be set when the row is created, never changed after.
//           an audit trail is only trustworthy if it can't be rewritten.
//           navigations named to match their FKs by convention: Claim + Id = ClaimId, ChangedByUser + Id = ChangedByUserId
// Related:  BenefitClaim (owns the list via _history / History), User (who made the change),
//           ClaimStatus enum, BenefitClaim.MoveTo (Day 5, the only thing that adds rows)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

/// <summary>
/// One status change on a claim: who changed it, when, from what, to what, and why.
/// Rows are only ever added, never edited. Together they form the claim's audit trail.
/// </summary>
public class ClaimStatusHistory
{
    // set (not init) on Id + navigations b/c EF Core fills those in itself
    public int Id { get; set; }

    // init = settable only while creating the object: new ClaimStatusHistory { ClaimId = 5, ... }
    public int ClaimId { get; init; }

    public BenefitClaim Claim { get; set; } = null!;

    /// <summary>The status before this change. Null on the first row, when the claim was submitted.</summary>
    public ClaimStatus? FromStatus { get; init; }

    public ClaimStatus ToStatus { get; init; }

    /// <summary>Who made the change: the member for the submission, an admin for every move after that.</summary>
    public int ChangedByUserId { get; init; }

    public User ChangedByUser { get; set; } = null!;

    public DateTime ChangedAt { get; init; }

    /// <summary>Optional explanation from the admin, e.g. why more info is needed.</summary>
    // string? = optional. null means "no note" (vs "" = a blank note), and the DB column allows NULL
    public string? Note { get; init; }
}
