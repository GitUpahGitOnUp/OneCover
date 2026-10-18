// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     entity for money moving between a member and the insurer (one row in Payments)
// Layer:    Models (domain)
// Concepts: all 3 kinds of setter in one class:
//             init        = facts fixed at creation (who, how much, when due), money records are never edited
//             private set = changes, but only through this class's own methods (MarkPaid/MarkFailed, Day 5)
//             set         = Id + navigations, EF Core fills those in
//           optional relationships: int? FK + nullable navigation (no = null!). each payment uses exactly one
// Related:  PaymentType, PaymentStatus, Enrollment (premiums), BenefitClaim (benefits), User,
//           EnrollmentService (creates premium payments, Day 4), claim approval (creates benefit payments, Day 5)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

/// <summary>
/// Money moving between a member and the insurer: a Premium (member pays for coverage)
/// or a Benefit (an approved claim pays the member). Appears on the member's statements.
/// </summary>
public class Payment
{
    public int Id { get; set; }

    public int UserId { get; init; }
    public User User { get; set; } = null!;

    /// <summary>Set for Premium payments: the coverage being paid for. Null for Benefit payments.</summary>
    public int? EnrollmentId { get; init; }
    public Enrollment? Enrollment { get; set; }

    /// <summary>Set for Benefit payments: the approved claim being paid out. Null for Premium payments.</summary>
    public int? ClaimId { get; init; }
    public BenefitClaim? Claim { get; set; }

    public PaymentType Type { get; init; }

    public decimal Amount { get; init; }

    public DateOnly DueDate { get; init; }

    /// <summary>When the payment went through. Only set when Status is Paid.</summary>
    public DateOnly? PaidDate { get; private set; }

    // rule to protect: Paid always comes w/ a PaidDate, and a Paid payment never goes back to Pending
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

    //private: outside code can't write "new Payment()". payments are created *ONLY* through factory methods
    // like PendingPremium, so every payment starts valid. (EF Core can still use this when loading from the DB)
    private Payment() { }

    /// <summary>Creates the Pending premium payment for a new enrollment.</summary>
    public static Payment PendingPremium(Enrollment enrollment, decimal amount, DateOnly dueDate) => new()
    {
        UserId = enrollment.UserId,
        Enrollment = enrollment, // link via the navigation, not EnrollmentId as the enrollment is brand new and no Id # has been saved yet
        Type = PaymentType.Premium,
        Amount = amount,
        DueDate = dueDate
        // Status is already Pending by default, and PaidDate stays null  
    };
}