// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     enum = a fixed list of named values. a ClaimStatus variable can only ever hold one of these 6.
// Layer:    Models (domain). used by entities, stored as text in SQL Server ("InReview", not 1)
// Concepts: type safety, the compiler catches typos like "Aproved" that a plain string never would
// Related:  BenefitClaim.Status, ClaimStatusHistory.FromStatus/ToStatus,
//           AppDbContext.ConfigureConventions (enums -> strings), ClaimWorkflow (Day 5: allowed moves)
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Models.Enums;


// Where a claim is in the review workflow. Which moves are allowed is decided by ClaimWorkflow.

public enum ClaimStatus
{
    Submitted,
    InReview,
    InfoRequested,
    Approved,
    Denied,
    Closed
}
