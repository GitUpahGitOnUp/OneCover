using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

public class BenefitClaim
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public Enrollment Enrollment { get; set; } = null!;
    public int EnrollmentId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public ClaimStatus Status { get; private set; } = ClaimStatus.Submitted;

    public decimal WeeklyBenefit { get; private set; }

    public decimal TotalBenefit { get; private set; }

    public DateTime SubmittedAt { get; set; }

    private readonly List<ClaimStatusHistory> _history = [];

    public IReadOnlyCollection<ClaimStatusHistory> History => _history;

}