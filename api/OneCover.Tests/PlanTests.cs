// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for Plan.IsDisability, the rule behind "claims only against STD/LTD coverage"
// Layer:    Tests (xUnit)
// Concepts: [Theory] + [InlineData] = ONE test method run once per data row (5 plan types -> 5 test cases),
//           instead of 5 copy-pasted [Fact]s. no database, no fakes: the rule lives on the entity, so it's easy to test
// Related:  Plan.IsDisability, ClaimService.SubmitAsync (rule 3 uses it), file-claim.ts (same filter in the UI)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Tests;

public class PlanTests
{
    [Theory]
    [InlineData(PlanType.STD, true)]
    [InlineData(PlanType.LTD, true)]
    [InlineData(PlanType.Life, false)]
    [InlineData(PlanType.Accident, false)]
    [InlineData(PlanType.Dental, false)]
    public void IsDisability_OnlyForShortAndLongTermDisability(PlanType type, bool expected)
    {
        var plan = new Plan { Type = type };

        Assert.Equal(expected, plan.IsDisability);
    }
}
