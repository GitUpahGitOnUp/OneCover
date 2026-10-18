// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO (data transfer object) for a plan going OUT to the client, the JSON shape of a plan
// Layer:    Dtos, the API boundary. the Plan entity stays inside the API, this is what crosses the wire
// Concepts: positional RECORD: the parameter list IS the type. one declaration creates
//             - a constructor taking all 9 values, in this exact order
//             - 9 read-only properties w/ the same names (Id, Name, Type...)
//           read-only b/c a response shouldn't be edited after it's built.
//           positional calls must follow this order, so PlanMappings uses named arguments to avoid mix-ups
// Related:  Plan (the entity it's built from), PlanMappings (does the building), PlanService + PlansController
//           (A3/A4, return these), Angular plan cards + admin plans table (Days 6 and 8, display them)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

/// <summary>A coverage plan as the API returns it. BenefitPercent is a fraction (0.60 = 60%).</summary>
// becomes JSON like { "id": 1, "name": "Short-Term Disability", "type": "STD", ... "isActive": true }
// (property names turn camelCase in JSON, and Type comes out as text thanks to JsonStringEnumConverter in Program.cs)
public record PlanDto(
    int Id, string Name, PlanType Type, string Description,
    decimal MonthlyPremium, decimal BenefitPercent, decimal WeeklyMaximum,
    int EliminationDays, bool IsActive   // IsActive included so the admin page can show retired plans too
);