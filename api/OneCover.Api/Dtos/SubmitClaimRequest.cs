// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for a new claim coming IN: the JSON body of POST /api/claims
// Layer:    Dtos, the API boundary
// Concepts: RECORD + validation attributes, nullable + [Required] on value types (a missing date is a 400,
//           not a silent 0001-01-01). no UserId: who's filing comes from the JWT. no amounts: the API calculates them
// Related:  ClaimsController (receives it), ClaimService.SubmitAsync, BenefitClaim.Submit (checks end >= start)
// ─────────────────────────────────────────────────────────────────────────────────────────
using System.ComponentModel.DataAnnotations;

namespace OneCover.Api.Dtos;

/// <summary>
/// A member's new claim against one of their enrollments. The member comes from the token,
/// and the benefit amounts are calculated by the API, never sent by the client.
/// </summary>
public record SubmitClaimRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? EnrollmentId { get; init; }

    [Required, MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;

    [Required]
    public DateOnly? StartDate { get; init; }

    // "end on or after start" isn't checked here: BenefitClaim.Submit owns that rule
    [Required]
    public DateOnly? EndDate { get; init; }
}
