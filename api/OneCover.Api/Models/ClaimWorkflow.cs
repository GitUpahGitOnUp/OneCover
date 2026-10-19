// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the claim status rulebook: which status a claim may move to from each status
// Layer:    Models (domain). pure rules: no database, no HTTP, no dependencies
// Concepts: ENCAPSULATION (the rules live in ONE private dictionary, reachable only through CanMove/NextStatuses),
//           static class, rules as DATA (a lookup table) instead of a pile of if statements,
//           TryGetValue so an unknown status is "no moves allowed" instead of a crash
// Related:  BenefitClaim.MoveTo (checks CanMove before every change), ClaimMappings (NextStatuses -> ClaimDto),
//           ClaimWorkflowTests, ClaimStatus enum
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

/// <summary>
/// The claim status rulebook: which status a claim can move to from each status.
/// BenefitClaim.MoveTo checks this before every change.
/// </summary>
public static class ClaimWorkflow
{
    // the rules live in *ONE* private place. Nothing outside this class can read or change the dictionary directly
    private static readonly Dictionary<ClaimStatus, ClaimStatus[]> Allowed = new()
    {
        [ClaimStatus.Submitted]     = [ClaimStatus.InReview],
        [ClaimStatus.InReview]      = [ClaimStatus.InfoRequested, ClaimStatus.Approved, ClaimStatus.Denied],
        [ClaimStatus.InfoRequested] = [ClaimStatus.InReview],
        [ClaimStatus.Approved]      = [ClaimStatus.Closed],
        [ClaimStatus.Denied]        = [ClaimStatus.Closed],
        [ClaimStatus.Closed]        = []                // final, no moves out of Closed
    };

    // TryGetValue instead of Allowed[from]: a status w/ no entry (say, one added to the enum later) means
    // "no moves allowed", so MoveTo gives a clean 400 instead of a KeyNotFoundException (500)
    /// <summary>True if a claim in status "from" is allowed to move to "to".</summary>
    public static bool CanMove(ClaimStatus from, ClaimStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    /// <summary>The statuses a claim can move to next. Used by the admin "Move to…" dropdown (Day 8).</summary>
    // read-only wrapper, so callers can't change the real rules. unknown status -> empty list
    public static IReadOnlyList<ClaimStatus> NextStatuses(ClaimStatus from) =>
        Allowed.TryGetValue(from, out var next) ? Array.AsReadOnly(next) : [];
}
