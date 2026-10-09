using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

/// <summary>
/// One status change on a claim: who changed it, when, from what, to what, and why.
/// Rows are only ever added, never edited. Together they form the claim's audit trail.
/// </summary>
public class ClaimStatusHistory
{
    public int Id { get; set; }

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
    public string? Note { get; init; }
}
