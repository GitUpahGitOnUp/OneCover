// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     SERVICE INTERFACE for claims: file a claim, list mine, get one
// Layer:    Services (business logic)
// Concepts: ABSTRACTION. no userId parameters: the service gets "who" from ICurrentUser (the JWT)
// Related:  ClaimService, ClaimsController, SubmitClaimRequest / ClaimDto
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;

namespace OneCover.Api.Services;

/// <summary>Claim logic for the logged-in user.</summary>
public interface IClaimService
{
    /// <summary>
    /// Files a claim against one of the member's own active enrollments.
    /// Throws NotFoundException if the enrollment isn't theirs, BusinessRuleException if it's inactive or the dates are invalid.
    /// </summary>
    Task<ClaimDto> SubmitAsync(SubmitClaimRequest request);

    /// <summary>The logged-in member's claims, newest first.</summary>
    Task<IReadOnlyList<ClaimDto>> GetMineAsync();

    /// <summary>One claim. Members can only see their own (404 otherwise); admins can see any.</summary>
    Task<ClaimDto> GetByIdAsync(int id);
}
