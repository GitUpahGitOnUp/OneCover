// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     entity for a coverage product (one row in Plans)
// Layer:    Models (domain)
// Concepts: plain { get; set; } everywhere = no encapsulation needed, admins can edit any field (no rules to protect).
//           one class for all plan types instead of inheritance (DisabilityPlan : Plan), simpler on purpose.
//           IsDisability = a computed property (no column) that answers "can claims be filed against this plan?"
// Related:  PlanType enum, Enrollment (many enrollments -> one plan), AppDbContext.Plans, DbSeeder (5 plans),
//           IPlanRepository / PlanService (Day 4)
// ─────────────────────────────────────────────────────────────────────────────────────────
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

    // decimal(18,2) in SQL, set once for every decimal in AppDbContext.ConfigureConventions
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
    // sensible default: a new plan starts active (a bool would otherwise default to false = born retired)
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// True for short- and long-term disability plans. Only these can have claims filed against them,
    /// because a claim here is a disability leave (leave dates, weekly benefit, waiting period).
    /// </summary>
    // a computed, read-only property (=> with no setter): EF doesn't map it to a column, it's derived from Type.
    // ENCAPSULATION: the plan answers "am I claimable?" itself, so the rule lives in one testable place
    public bool IsDisability => Type is PlanType.STD or PlanType.LTD;
}
