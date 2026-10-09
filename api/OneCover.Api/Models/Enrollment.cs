using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

/// <summary>
/// A member's sign-up for a coverage plan. Connects a User to a Plan.
/// Claims are filed against an enrollment, and premium payments are billed for one.
/// </summary>

public class Enrollment
{
    public int Id  { get; set; }

    public int UserId { get; set; } 

    public User User { get; set; } = null!;

    public int PlanId { get; set; }

    public Plan Plan { get; set; } = null!;

    /// <summary>The date coverage started.</summary>
    public DateOnly EnrolledOn { get; set; } 

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}

