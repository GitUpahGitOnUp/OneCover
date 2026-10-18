// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for an enrollment going OUT: one row of the member's "My Coverage" list
// Layer:    Dtos, the API boundary
// Concepts: positional RECORD. FLATTENED on purpose: instead of nesting a whole PlanDto, it carries just the
//           plan fields the member's coverage list needs (name, type, premium), so the JSON stays simple
// Related:  Enrollment + Plan (built from), EnrollmentMappings (does the building), EnrollmentService,
//           EnrollmentsController, Angular "My Coverage" page (Day 7)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

/// <summary>A member's enrollment, with the key details of the plan it's for.</summary>
public record EnrollmentDto(
    int Id,
    int PlanId,
    string PlanName,
    PlanType PlanType,
    decimal MonthlyPremium,
    DateOnly EnrolledOn,
    EnrollmentStatus Status);
