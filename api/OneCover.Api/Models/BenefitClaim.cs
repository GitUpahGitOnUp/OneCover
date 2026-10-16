// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     entity for a member's request for benefits (one row in Claims). named BenefitClaim, not Claim,
//           to avoid clashing w/ System.Security.Claims.Claim (used for JWTs in AuthService)
// Layer:    Models (domain)
// Concepts: ENCAPSULATION. this is the one entity w/ workflow rules, so it protects its own data:
//           private set on Status + the calculated amounts, a private history list exposed read-only.
//           "rich domain model": the class enforces its own rules (MoveTo on Day 5) vs. any code setting anything
// Related:  ClaimStatus enum, ClaimStatusHistory (its audit trail), Enrollment, User,
//           ClaimWorkflow + MoveTo (Day 5), BenefitClaim.Submit factory (Day 4), BenefitCalculator (Day 5)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

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

}
