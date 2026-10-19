// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the STD/LTD benefit math. pure functions: same inputs -> same outputs, no DB, no clock
// Layer:    Services (business logic)
// Concepts: implements an INTERFACE, decimal math for money (never double), Math.Round to cents,
//           Math.Min for the weekly cap, Math.Max(0, ...) so a short leave can't go negative.
//           registered as a SINGLETON in Program.cs b/c it has no state (one shared instance is safe)
// Related:  IBenefitCalculator, ClaimService (calls it on submit), BenefitCalculatorTests
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Services;

public class BenefitCalculator : IBenefitCalculator
{
    // example: $78,000 salary, 60% plan, $1,500 weekly max
    //   78000 / 52 = 1500 a week -> 1500 x 0.60 = 900 -> under the 1,500 cap -> 900.00
    // for non-disability plans (Life/Accident/Dental) the percent and max are 0, so this returns 0
    public decimal WeeklyBenefit(decimal annualSalary, decimal benefitPercent, decimal weeklyMaximum)
    {
        var weeklySalary = annualSalary / 52m;                       // 52 weeks in a year. the m makes it a decimal
        var benefit = Math.Round(weeklySalary * benefitPercent, 2);  // round to cents
        return Math.Min(benefit, weeklyMaximum);                     // never more than the plan's weekly cap
    }

    // example: $700/week, leave Oct 1 -> Oct 14 (14 days counting both ends), 7-day elimination period
    //   14 - 7 = 7 payable days -> 700 / 7 = 100 a day -> 7 x 100 = 700.00
    public decimal TotalBenefit(decimal weeklyBenefit, DateOnly startDate, DateOnly endDate, int eliminationDays)
    {
        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;   // DayNumber = days since 0001-01-01. +1 counts both ends
        var payableDays = Math.Max(0, totalDays - eliminationDays);    // the waiting period isn't paid. never below 0
        return Math.Round(weeklyBenefit / 7m * payableDays, 2);        // daily rate x payable days, rounded to cents
    }
}
