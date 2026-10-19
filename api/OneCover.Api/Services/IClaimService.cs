// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     SERVICE INTERFACE for claims: member actions (file, list mine, get one) + admin actions
//           (paged queue, move a claim through the workflow, dashboard counts)
// Layer:    Services (business logic)
// Concepts: ABSTRACTION. no userId parameters: the service gets "who" from ICurrentUser (the JWT)
// Related:  ClaimService, ClaimsController (member), AdminClaimsController + AdminDashboardController (admin)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Services;

/// <summary>Claim logic for members and admins.</summary>
public interface IClaimService
{
    /// <summary>
    /// Files a claim against one of the member's own active enrollments, with the benefit calculated.
    /// Throws NotFoundException if the enrollment isn't theirs, BusinessRuleException if it's inactive or the dates are invalid.
    /// </summary>
    Task<ClaimDto> SubmitAsync(SubmitClaimRequest request);

    /// <summary>The logged-in member's claims, newest first.</summary>
    Task<IReadOnlyList<ClaimDto>> GetMineAsync();

    /// <summary>One claim. Members can only see their own (404 otherwise); admins can see any.</summary>
    Task<ClaimDto> GetByIdAsync(int id);

    /// <summary>Admin queue: one page of claims, newest first, optionally filtered to one status.</summary>
    Task<PagedResult<ClaimDto>> GetPageAsync(ClaimStatus? status, int page, int pageSize);

    /// <summary>
    /// Admin: moves a claim to a new status (via BenefitClaim.MoveTo). Approving creates a Pending benefit payment.
    /// Throws NotFoundException if the claim doesn't exist, BusinessRuleException if the workflow doesn't allow the move.
    /// </summary>
    Task<ClaimDto> UpdateStatusAsync(int id, UpdateClaimStatusRequest request);

    /// <summary>Admin dashboard: claim counts for every status (zeros included).</summary>
    Task<DashboardDto> GetDashboardAsync();
}
