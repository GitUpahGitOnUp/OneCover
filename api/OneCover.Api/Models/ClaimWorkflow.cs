using System.Security.Claims;
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

    /// <summary>True if a claim in status "from" is allowed to move to "to".</summary>
    public static bool CanMove(ClaimStatus from, ClaimStatus to) => Allowed[from].Contains(to);

    /// <summary>The statuses a claim can move to next. Used by the admin "Move to…" dropdown (Day 8).</summary>
    public static IReadOnlyList<ClaimStatus> NextStatuses(ClaimStatus from) => Array.AsReadOnly(Allowed[from]);
}