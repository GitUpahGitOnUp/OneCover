// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     INTERFACE for FMLA eligibility (Family and Medical Leave Act: job-protected unpaid leave)
// Layer:    Services (business logic)
// Concepts: ABSTRACTION. the rule: 12+ months employed AND 1,250+ hours worked in the last 12 months
// Related:  EligibilityService, ProfileService (/api/me shows the result), EligibilityServiceTests
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Services;

/// <summary>Checks leave eligibility rules.</summary>
public interface IEligibilityService
{
    /// <summary>True if employed 12+ months (as of today) AND worked 1,250+ hours in the last 12 months.</summary>
    bool IsFmlaEligible(DateOnly hireDate, int hoursWorkedLast12Months);
}
