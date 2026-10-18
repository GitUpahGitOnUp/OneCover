// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     Enrollment entity -> EnrollmentDto
// Layer:    Dtos (mapping)
// Concepts: EXTENSION METHOD (enrollment.ToDto()), named arguments (no silent swaps), uses the Plan NAVIGATION,
//           so the Plan must be loaded first (Include in the repository, or set in the service), or it'd be null
// Related:  Enrollment, Plan, EnrollmentDto, EnrollmentRepository (Include(e => e.Plan)), EnrollmentService
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;

namespace OneCover.Api.Dtos;

public static class EnrollmentMappings
{
    /// <summary>Converts an Enrollment (with its Plan loaded) to the DTO the API returns.</summary>
    public static EnrollmentDto ToDto(this Enrollment enrollment) => new(
        Id: enrollment.Id,
        PlanId: enrollment.PlanId,
        PlanName: enrollment.Plan.Name,              // via the navigation property
        PlanType: enrollment.Plan.Type,
        MonthlyPremium: enrollment.Plan.MonthlyPremium,
        EnrolledOn: enrollment.EnrolledOn,
        Status: enrollment.Status);
}
