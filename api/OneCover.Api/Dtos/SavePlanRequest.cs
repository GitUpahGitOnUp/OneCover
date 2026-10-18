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

    // [EnumDataType] = must be a REAL PlanType member. JSON can send an enum as a number, and [Required] alone
    // would accept "type": 99 (present, but not a real plan type)
    [Required, EnumDataType(typeof(PlanType))]
    public PlanType? Type { get; init; } // nullable here as missing 'type' would silently become first value, STD. Nullable + Required returns a 400 instead

    // optional. string? (nullable) matters: w/ nullable reference types on, ASP.NET Core treats a plain non-nullable
    // string as IMPLICITLY [Required], which would reject a plan w/ no description
    [MaxLength(1000)]
    public string? Description { get; init; }

    // [Range(typeof(decimal), "min", "max")] = the decimal-aware Range. the plain Range(0, 10_000) is the INT version:
    // it rounds the value to a whole number before checking, so 1.4 -> 1 would pass a 0..1 check and -0.4 -> 0
    // would pass a "no negatives" check. min/max are strings b/c attribute arguments can't be decimals
    [Required, Range(typeof(decimal), "0", "10000")]
    public decimal? MonthlyPremium { get; init; }

    [Required, Range(typeof(decimal), "0", "1")]
    public decimal? BenefitPercent { get; init; } // range 0,1 catches someone enter 60 instead of 0.60

    // 0 for non-disability plans (Life, Accident, Dental), which is why 0 is inside every range
    [Required, Range(typeof(decimal), "0", "100000")]
    public decimal? WeeklyMaximum { get; init; }

    // waiting period in days, a year at most
    [Required, Range(0, 365)]
    public int? EliminationDays { get; init; }
}