// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     entity for money moving between a member and the insurer (one row in Payments)
// Layer:    Models (domain)
// Concepts: all 3 kinds of setter in one class:
//             init        = facts fixed at creation (who, how much, when due), money records are never edited
//             private set = changes, but only through this class's own methods (MarkPaid/MarkFailed)
//           FACTORY METHODS + PRIVATE CONSTRUCTOR: PendingPremium / PendingBenefit are the only ways to create one
//             set         = Id + navigations, EF Core fills those in
//           optional relationships: int? FK + nullable navigation (no = null!). each payment uses exactly one
// Related:  PaymentType, PaymentStatus, Enrollment (premiums), BenefitClaim (benefits), User,
//           EnrollmentService (creates premium payments, Day 4), claim approval (creates benefit payments, Day 5)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Errors;
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

    /// <summary>Creates the Pending benefit payment for an approved claim, for the claim's total benefit.</summary>
    // ClaimId (not the navigation) b/c the claim already exists, so its real id is known (same reasoning as BenefitClaim.Submit)
    public static Payment PendingBenefit(BenefitClaim claim, DateOnly dueDate) => new()
    {
        UserId = claim.UserId,
        ClaimId = claim.Id,
        Type = PaymentType.Benefit,
        Amount = claim.TotalBenefit,
        DueDate = dueDate
    };

    /// <summary>Marks a Pending payment as paid on the given date. Throws BusinessRuleException if it isn't Pending.</summary>
    // the rule this class protects: Status and PaidDate change TOGETHER, and only from Pending.
    // Status/PaidDate have private setters, so these methods are the only way in (encapsulation)
    public void MarkPaid(DateOnly paidOn)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new BusinessRuleException($"Only pending payments can be marked paid. This one is {Status}.");
        }

        Status = PaymentStatus.Paid;
        PaidDate = paidOn;
    }

    /// <summary>Marks a Pending payment as failed. Throws BusinessRuleException if it isn't Pending.</summary>
    public void MarkFailed()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new BusinessRuleException($"Only pending payments can be marked failed. This one is {Status}.");
        }

        Status = PaymentStatus.Failed;   // PaidDate stays null: it never went through
    }
}