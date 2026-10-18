// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for a plan coming IN from an admin: the JSON body of POST /api/plans (create)
//           AND PUT /api/plans/{id} (update). same fields both ways, so one class keeps them in sync (DRY)
// Layer:    Dtos, the API boundary
// Concepts: RECORD w/ init properties (set once when the JSON is read, can't change after).
//           VALIDATION ATTRIBUTES = "validate at the edges": [ApiController] checks every one of these
//           BEFORE the controller method runs, and sends back a 400 w/ an errors list if anything fails.
//           the nullable + [Required] trick on value types (PlanType?, decimal?, int?)
// Related:  PlansController (receives it, A4), PlanService (copies it onto a Plan entity, A3), Plan,
//           AppDbContext (the DB max lengths these attributes match: Name 100, Description 1000)
// ─────────────────────────────────────────────────────────────────────────────────────────
using System.ComponentModel.DataAnnotations;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

/// <summary>
/// An admin's create or update request for a plan. BenefitPercent is a fraction (0.60 = 60%).
/// IsActive isn't here on purpose: retiring a plan has its own endpoint (DELETE).
/// </summary>
public record SavePlanRequest
{
    // [Required] = must be present and not empty. [MaxLength(100)] = same limit as the DB column
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public PlanType? Type { get; init; } // nullable here as missing 'type' would silently become first value, STD. Nullable + Required returns a 400 instead

    // optional: no [Required], so an empty description is allowed
    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    // [Range(min, max)] = the value must fall between these, inclusive. 10_000 = 10,000 (the _ is just for readability)
    [Required, Range(0, 10_000)]
    public decimal? MonthlyPremium { get; init; }

    [Required, Range(0, 1)]
    public decimal? BenefitPercent { get; init; } // range 0,1 catches someone enter 60 instead of 0.60

    // 0 for non-disability plans (Life, Accident, Dental), which is why 0 is inside every range
    [Required, Range(0, 100_000)]
    public decimal? WeeklyMaximum { get; init; }

    // waiting period in days, a year at most
    [Required, Range(0, 365)]
    public int? EliminationDays { get; init; }
}