// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for an enroll request coming IN: the JSON body of POST /api/enrollments, e.g. { "planId": 1 }
// Layer:    Dtos, the API boundary
// Concepts: RECORD + validation attributes. note what's NOT here: no UserId. who is enrolling always comes from
//           the JWT (ICurrentUser), never the request body, so a member can't enroll someone else
// Related:  EnrollmentsController (receives it), EnrollmentService.EnrollAsync (uses it)
// ─────────────────────────────────────────────────────────────────────────────────────────
using System.ComponentModel.DataAnnotations;

namespace OneCover.Api.Dtos;

/// <summary>Enroll the logged-in member in a plan. The member comes from the token, not this request.</summary>
public record EnrollRequest
{
    // int? + [Required] = the same trick as the other DTOs: a missing planId is a 400, not a silent 0.
    // Range(1, int.MaxValue) = ids start at 1, so 0 or negatives are rejected before the service runs
    [Required, Range(1, int.MaxValue)]
    public int? PlanId { get; init; }
}
