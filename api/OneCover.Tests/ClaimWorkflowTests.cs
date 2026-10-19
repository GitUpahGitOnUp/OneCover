// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for the claim status rulebook (which moves are allowed)
// Layer:    Tests (xUnit). runs w/ `dotnet test` from api/, no database or web server needed
// Concepts: [Fact] = one test. [Theory] + [InlineData] = the SAME test run once per data row, so every
//           allowed/blocked move is checked w/o copy-pasting a test for each. Arrange / Act / Assert
// Related:  ClaimWorkflow (what's tested), BenefitClaim.MoveTo (uses it)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Tests;

public class ClaimWorkflowTests
{
    // every move the workflow diagram allows
    [Theory]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.InReview)]
    [InlineData(ClaimStatus.InReview, ClaimStatus.InfoRequested)]
    [InlineData(ClaimStatus.InReview, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.InReview, ClaimStatus.Denied)]
    [InlineData(ClaimStatus.InfoRequested, ClaimStatus.InReview)]
    [InlineData(ClaimStatus.Approved, ClaimStatus.Closed)]
    [InlineData(ClaimStatus.Denied, ClaimStatus.Closed)]
    public void CanMove_AllowsValidMoves(ClaimStatus from, ClaimStatus to)
    {
        Assert.True(ClaimWorkflow.CanMove(from, to));
    }

    // a sample of moves that must be blocked: skipping review, reopening, flipping a decision
    [Theory]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.Approved)]   // can't skip review
    [InlineData(ClaimStatus.Closed, ClaimStatus.InReview)]      // closed is final
    [InlineData(ClaimStatus.Denied, ClaimStatus.Approved)]      // a decision can't be flipped
    [InlineData(ClaimStatus.Approved, ClaimStatus.Denied)]
    [InlineData(ClaimStatus.InfoRequested, ClaimStatus.Approved)]
    public void CanMove_BlocksInvalidMoves(ClaimStatus from, ClaimStatus to)
    {
        Assert.False(ClaimWorkflow.CanMove(from, to));
    }

    [Fact]
    public void NextStatuses_IsEmpty_ForClosed()
    {
        Assert.Empty(ClaimWorkflow.NextStatuses(ClaimStatus.Closed));
    }

    [Fact]
    public void NextStatuses_ListsAllThreeOptions_FromInReview()
    {
        var next = ClaimWorkflow.NextStatuses(ClaimStatus.InReview);

        Assert.Equal([ClaimStatus.InfoRequested, ClaimStatus.Approved, ClaimStatus.Denied], next);
    }
}
