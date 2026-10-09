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

    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
}