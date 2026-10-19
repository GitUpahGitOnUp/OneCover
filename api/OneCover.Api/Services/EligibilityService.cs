// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the FMLA eligibility rule
// Layer:    Services (business logic)
// Concepts: DEPENDENCY INJECTION of TimeProvider: "today" comes from the injected clock, NOT DateTime.UtcNow,
//           so tests can pin today to any date (the "exactly 12 months" edge case is easy to test).
//           singleton lifetime (no per-request state)
// Related:  IEligibilityService, ProfileService, Program.cs (AddSingleton(TimeProvider.System)), EligibilityServiceTests
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Services;

public class EligibilityService(TimeProvider time) : IEligibilityService
{
    private const int RequiredHours = 1250;   // named constant instead of a "magic number" in the rule

    public bool IsFmlaEligible(DateOnly hireDate, int hoursWorkedLast12Months)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        // hired at least 12 months ago: hire date + 12 months must be today or earlier
        var employedTwelveMonths = hireDate.AddMonths(12) <= today;

        return employedTwelveMonths && hoursWorkedLast12Months >= RequiredHours;
    }
}
