// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     all plan mapping, both directions:
//             ToDto()   = Plan entity -> PlanDto        (OUT: what the client sees)
//             ApplyTo() = SavePlanRequest -> Plan entity (IN: copies an admin's create/update onto the entity)
// Layer:    Dtos (mapping between the domain layer and the API boundary)
// Concepts: EXTENSION METHOD: a static method that LOOKS like it belongs to Plan. thanks to 'this' on the
//           first parameter you can write plan.ToDto() instead of PlanMappings.ToDto(plan), w/o touching Plan.cs.
//           dependency direction: Dtos know about Models, Models never know about Dtos (so ToDto lives HERE,
//           not inside Plan). hand-written mapping = compile-time safe (vs AutoMapper, which maps at runtime)
// Related:  Plan, PlanDto, SavePlanRequest, PlanService (calls plan.ToDto() + request.ApplyTo(plan))
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;

namespace OneCover.Api.Dtos;

// static class = never instantiated, just holds static methods. extension methods MUST live in one
public static class PlanMappings
{
    /// <summary>Converts a Plan entity to the DTO the API returns.</summary>
    // anatomy of the signature:
    //   public static   = callable w/o an object (required for extension methods)
    //   PlanDto         = what it returns
    //   ToDto           = the method name
    //   this Plan plan  = 'this' makes it an extension method ON Plan. plan = the object it's called on
    //   => new(...)     = expression body: "return a new PlanDto built from these values".
    //                     new(...) w/o the type name works b/c C# knows the return type is PlanDto
    // named arguments (Id: ..., Name: ...): each value is labeled, so order doesn't matter
    // and two values of the same type (e.g. two decimals) can't be silently swapped
    public static PlanDto ToDto(this Plan plan) => new(
        Id: plan.Id,
        Name: plan.Name,
        Type: plan.Type,
        Description: plan.Description,
        MonthlyPremium: plan.MonthlyPremium,
        BenefitPercent: plan.BenefitPercent,
        WeeklyMaximum: plan.WeeklyMaximum,
        EliminationDays: plan.EliminationDays,
        IsActive: plan.IsActive);

    /// <summary> Copies a create/update request onto a Plan entity (new or loaded from the DB).</summary>
    // extension method on SavePlanRequest: request.ApplyTo(plan). returns void b/c it changes the plan it's given.
    // create AND update both use it, so the copy logic exists exactly once
    public static void ApplyTo(this SavePlanRequest request, Plan plan)
    {
        plan.Name = request.Name.Trim();
        plan.Type = request.Type!.Value;
        plan.Description = request.Description.Trim();
        plan.MonthlyPremium = request.MonthlyPremium!.Value; // all of the '!' are safe as [Required] already ran
        plan.BenefitPercent = request.BenefitPercent!.Value;
        plan.WeeklyMaximum = request.WeeklyMaximum!.Value;
        plan.EliminationDays = request.EliminationDays!.Value;
    }
}