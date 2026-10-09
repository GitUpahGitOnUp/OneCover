using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

/// <summary>
/// A coverage product members can enroll in like short-term disability, dental
/// Plans cannot be deleted but can be retired, because enrollments still point to them
/// </summary>
public class Plan
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public PlanType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal MonthlyPremium { get; set; }

    /// <summary>
    /// Fraction of weekly salary paid as a benefit, e.g. 0.60 = 60%.
    /// Disability plans (STD/LTD) only; 0 for other plan types.
    /// </summary>
    public decimal BenefitPercent { get; set; }

    public decimal WeeklyMaximum { get; set; }

    /// <summary>
    /// Waiting period: the first days of a leave that aren't paid.
    /// E.g. 7 means benefits start on day 8. STD/LTD only.
    /// </summary>
    public int EliminationDays { get; set; }

    /// <summary>False when an admin retires the plan. Retired plans are hidden from members
    ///  but kept in the database.</summary>
    public bool IsActive { get; set; } = true;
}