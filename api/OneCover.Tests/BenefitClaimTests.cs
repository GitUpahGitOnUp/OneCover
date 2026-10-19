// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for the BenefitClaim entity's own rules: Submit (creating) and MoveTo (changing status)
// Layer:    Tests (xUnit)
// Concepts: testing ENCAPSULATION: the entity is tested directly, no database, b/c its rules live inside it.
//           Assert.Throws<T> checks that a rule is enforced, then we check nothing changed (no half-done update)
// Related:  BenefitClaim, ClaimWorkflow, ClaimStatusHistory, BusinessRuleException
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Errors;
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Tests;

public class BenefitClaimTests
{
    private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    // an existing enrollment for member #2, coverage starting Jan 1, 2026
    private static Enrollment ActiveEnrollment() => new()
    {
        Id = 1,
        UserId = 2,
        PlanId = 1,
        EnrolledOn = new DateOnly(2026, 1, 1)
    };

    private static BenefitClaim SubmittedClaim() => BenefitClaim.Submit(
        ActiveEnrollment(), "Knee surgery", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 14),
        weeklyBenefit: 900m, totalBenefit: 900m, submittedAt: Now);

    [Fact]
    public void Submit_StartsAsSubmitted_WithOneHistoryRow()
    {
        var claim = SubmittedClaim();

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        var row = Assert.Single(claim.History);       // exactly one row, and hand it back to check
        Assert.Null(row.FromStatus);                   // the first row has no "from"
        Assert.Equal(ClaimStatus.Submitted, row.ToStatus);
        Assert.Equal(2, row.ChangedByUserId);          // submitted by the member
    }

    [Fact]
    public void Submit_Throws_WhenEndDateIsBeforeStartDate()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => BenefitClaim.Submit(
            ActiveEnrollment(), "Flu", new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 1), 0m, 0m, Now));

        Assert.Contains("End date", ex.Message);
    }

    [Fact]
    public void Submit_Throws_WhenClaimCoversMoreThanOneYear()
    {
        // an end date like 9999-12-31 would otherwise calculate a huge benefit
        Assert.Throws<BusinessRuleException>(() => BenefitClaim.Submit(
            ActiveEnrollment(), "Long leave", new DateOnly(2026, 2, 1), new DateOnly(9999, 12, 31), 0m, 0m, Now));
    }

    [Fact]
    public void Submit_Throws_WhenClaimStartsBeforeCoverage()
    {
        // coverage began Jan 1, 2026, so a leave starting in 2025 isn't covered
        Assert.Throws<BusinessRuleException>(() => BenefitClaim.Submit(
            ActiveEnrollment(), "Old injury", new DateOnly(2025, 12, 1), new DateOnly(2025, 12, 20), 0m, 0m, Now));
    }

    [Fact]
    public void MoveTo_ValidMove_ChangesStatus_AndRecordsHistory()
    {
        var claim = SubmittedClaim();

        claim.MoveTo(ClaimStatus.InReview, changedByUserId: 1, note: "  Starting review  ", changedAt: Now);

        Assert.Equal(ClaimStatus.InReview, claim.Status);
        Assert.Equal(2, claim.History.Count);
        var latest = claim.History.Last();
        Assert.Equal(ClaimStatus.Submitted, latest.FromStatus);   // recorded BEFORE the status changed
        Assert.Equal(ClaimStatus.InReview, latest.ToStatus);
        Assert.Equal(1, latest.ChangedByUserId);                  // the admin
        Assert.Equal("Starting review", latest.Note);             // trimmed
    }

    [Fact]
    public void MoveTo_InvalidMove_Throws_AndChangesNothing()
    {
        var claim = SubmittedClaim();

        // Submitted -> Approved skips review, which the workflow doesn't allow
        Assert.Throws<BusinessRuleException>(() =>
            claim.MoveTo(ClaimStatus.Approved, changedByUserId: 1, note: null, changedAt: Now));

        Assert.Equal(ClaimStatus.Submitted, claim.Status);   // status untouched
        Assert.Single(claim.History);                        // no history row written
    }

    [Fact]
    public void MoveTo_BlankNote_IsStoredAsNull()
    {
        var claim = SubmittedClaim();

        claim.MoveTo(ClaimStatus.InReview, changedByUserId: 1, note: "   ", changedAt: Now);

        Assert.Null(claim.History.Last().Note);
    }
}
