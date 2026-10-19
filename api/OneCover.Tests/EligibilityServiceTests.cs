// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for FMLA eligibility (12+ months employed AND 1,250+ hours)
// Layer:    Tests (xUnit)
// Concepts: this is WHY TimeProvider is injected: FixedTimeProvider pins "today" to one date, so the test gives
//           the same answer no matter what day it runs. INHERITANCE + POLYMORPHISM: it overrides
//           TimeProvider.GetUtcNow, and EligibilityService can't tell it's not the real clock.
//           edge cases on purpose: EXACTLY 12 months, EXACTLY 1,250 hours, one day/hour short
// Related:  EligibilityService, IEligibilityService, ProfileService (/api/me uses the result)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Services;

namespace OneCover.Tests;

public class EligibilityServiceTests
{
    // a fake clock: always returns the date it was given. a tiny subclass instead of an extra test package
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    // "today" for every test: Oct 19, 2026
    private readonly EligibilityService _service =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void IsEligible_WhenExactlyTwelveMonths_AndExactly1250Hours()
    {
        Assert.True(_service.IsFmlaEligible(hireDate: new DateOnly(2025, 10, 19), hoursWorkedLast12Months: 1250));
    }

    [Fact]
    public void IsNotEligible_WhenOneDayShortOfTwelveMonths()
    {
        Assert.False(_service.IsFmlaEligible(hireDate: new DateOnly(2025, 10, 20), hoursWorkedLast12Months: 2000));
    }

    [Fact]
    public void IsNotEligible_WhenOneHourShortOf1250()
    {
        Assert.False(_service.IsFmlaEligible(hireDate: new DateOnly(2020, 1, 1), hoursWorkedLast12Months: 1249));
    }

    [Fact]
    public void IsNotEligible_ForANewHire_LikeTheSeededNewHireUser()
    {
        // hired 6 months ago w/ 900 hours: fails both rules
        Assert.False(_service.IsFmlaEligible(hireDate: new DateOnly(2026, 4, 19), hoursWorkedLast12Months: 900));
    }
}
