// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     entity for a member's request for benefits (one row in Claims). named BenefitClaim, not Claim,
//           to avoid clashing w/ System.Security.Claims.Claim (used for JWTs in AuthService)
// Layer:    Models (domain)
// Concepts: ENCAPSULATION. this is the one entity w/ workflow rules, so it protects its own data:
//           private set on Status + the calculated amounts, a private history list exposed read-only.
//           "rich domain model": the class enforces its own rules (MoveTo on Day 5) vs. any code setting anything.
//           FACTORY METHOD + PRIVATE CONSTRUCTOR: Submit() is the ONLY way to create a claim, so every claim starts
//           valid (dates checked, status Submitted, first history row written)
// Related:  ClaimStatus enum, ClaimStatusHistory (its audit trail), Enrollment, User, BusinessRuleException,
//           ClaimService (calls Submit), ClaimWorkflow + MoveTo (Day 5), BenefitCalculator (Day 5, supplies the amounts)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;
using OneCover.Api.Errors;

namespace OneCover.Api.Models;

public class BenefitClaim
{
    public int Id { get; set; }

    // stored directly (also reachable via Enrollment.UserId) so "my claims" can filter w/o a join, small denormalization
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public Enrollment Enrollment { get; set; } = null!;
    public int EnrollmentId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    // private set = only code inside this class can change it. outside code can read it, not write it.
    // EF Core can still set it when loading from the DB
    public ClaimStatus Status { get; private set; } = ClaimStatus.Submitted;

    // calculated once at submission, never edited after
    public decimal WeeklyBenefit { get; private set; }

    public decimal TotalBenefit { get; private set; }

    public DateTime SubmittedAt { get; set; }

    // the real list, private: only this class can add to it.
    // EF finds it by convention (_ + the property name in camelCase)
    private readonly List<ClaimStatusHistory> _history = [];

    // collection navigation exposed as read-only: IReadOnlyCollection has no Add method
    public IReadOnlyCollection<ClaimStatusHistory> History => _history;

    // private: no "new BenefitClaim()" from outside. Submit is the only way to create a claim.
    // EF Core can still call it when loading claims from the DB
    private BenefitClaim() { }

    /// <summary>
    /// Creates a new claim in Submitted status, with its first history row.
    /// Throws BusinessRuleException if the end date is before the start date.
    /// </summary>
    // static = called on the class, not an object: BenefitClaim.Submit(...). returns the new claim.
    // the amounts are passed in (0 for now). on Day 5 the BenefitCalculator supplies real ones, Submit doesn't change
    public static BenefitClaim Submit(
        Enrollment enrollment, string reason, DateOnly startDate, DateOnly endDate,
        decimal weeklyBenefit, decimal totalBenefit, DateTime submittedAt)
    {
        // the claim protects its own rule, no matter who calls Submit (Angular checks it too, this is the last line of defense)
        if (endDate < startDate)
        {
            throw new BusinessRuleException("End date can't be before the start date.");
        }

        var claim = new BenefitClaim
        {
            UserId = enrollment.UserId,
            EnrollmentId = enrollment.Id,     // the enrollment already exists, so its real id is known
            Reason = reason.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            WeeklyBenefit = weeklyBenefit,    // private set: allowed here because we're INSIDE the class
            TotalBenefit = totalBenefit,
            SubmittedAt = submittedAt
            // Status already defaults to Submitted
        };

        // first audit row: no "from" status (the claim didn't exist before), submitted by the member.
        // only code inside BenefitClaim can reach the private _history list. on save, EF inserts the claim
        // first, then this row, filling in its ClaimId automatically
        claim._history.Add(new ClaimStatusHistory
        {
            FromStatus = null,
            ToStatus = ClaimStatus.Submitted,
            ChangedByUserId = enrollment.UserId,
            ChangedAt = submittedAt
        });

        return claim;

    }

}
