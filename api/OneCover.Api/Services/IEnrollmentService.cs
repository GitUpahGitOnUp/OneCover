// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     SERVICE INTERFACE for enrollments: enroll the logged-in member, list their coverage
// Layer:    Services (business logic)
// Concepts: ABSTRACTION. notice no userId parameters: the service gets the member from ICurrentUser (the JWT)
// Related:  EnrollmentService, EnrollmentsController, EnrollRequest / EnrollmentDto
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;

namespace OneCover.Api.Services;

/// <summary>Enrollment logic for the logged-in member.</summary>
public interface IEnrollmentService
{
    /// <summary>
    /// Enrolls the logged-in member in a plan and creates their first Pending premium payment.
    /// Throws NotFoundException if the plan doesn't exist, BusinessRuleException if it's retired or they're already enrolled.
    /// </summary>
    Task<EnrollmentDto> EnrollAsync(EnrollRequest request);

    /// <summary>The logged-in member's enrollments, newest first.</summary>
    Task<IReadOnlyList<EnrollmentDto>> GetMineAsync();
}
