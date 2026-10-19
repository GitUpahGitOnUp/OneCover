// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for an admin moving a claim: the JSON body of PUT /api/admin/claims/{id}/status
// Layer:    Dtos, the API boundary
// Concepts: validation attributes. note what's NOT checked here: whether the move is allowed.
//           that's ClaimWorkflow's job, enforced inside BenefitClaim.MoveTo (the entity owns its rules)
// Related:  AdminClaimsController, ClaimService.UpdateStatusAsync, BenefitClaim.MoveTo
// ─────────────────────────────────────────────────────────────────────────────────────────
using System.ComponentModel.DataAnnotations;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

/// <summary>An admin's request to move a claim to a new status, with an optional note for the history.</summary>
public record UpdateClaimStatusRequest
{
    // must be a real ClaimStatus. whether this particular move is ALLOWED is decided by the workflow, not here
    [Required, EnumDataType(typeof(ClaimStatus))]
    public ClaimStatus? Status { get; init; }

    // optional explanation, e.g. "Please upload the doctor's note". shows in the claim's history timeline
    [MaxLength(1000)]
    public string? Note { get; init; }
}
