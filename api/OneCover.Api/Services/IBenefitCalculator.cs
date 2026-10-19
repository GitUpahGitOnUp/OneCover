// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     INTERFACE for the disability benefit math (weekly benefit + total benefit for a claim)
// Layer:    Services (pure business logic: no database, no HTTP)
// Concepts: ABSTRACTION + POLYMORPHISM: ClaimService depends on this interface, so a test or a future
//           "LTD calculator" could swap in a different implementation without touching ClaimService
// Related:  BenefitCalculator (implementation), ClaimService.SubmitAsync (caller), BenefitCalculatorTests,
//           Plan (BenefitPercent, WeeklyMaximum, EliminationDays), User.AnnualSalary
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Services;

/// <summary>Calculates disability benefits from salary, the plan's rules, and the claim's dates.</summary>
public interface IBenefitCalculator
{
    /// <summary>Weekly benefit: weekly salary × benefit percent, capped at the plan's weekly maximum.</summary>
    decimal WeeklyBenefit(decimal annualSalary, decimal benefitPercent, decimal weeklyMaximum);

    /// <summary>
    /// Total benefit for the claim period, after the unpaid elimination (waiting) period.
    /// Start and end dates are both counted. Returns 0 if the leave is shorter than the elimination period.
    /// </summary>
    decimal TotalBenefit(decimal weeklyBenefit, DateOnly startDate, DateOnly endDate, int eliminationDays);
}
