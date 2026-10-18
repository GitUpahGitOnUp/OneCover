// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the business logic for plans: list, get, create, update, retire
// Layer:    Services. controller above it (HTTP), repository below it (data). this layer knows neither HTTP nor SQL
// Concepts: implements an INTERFACE (: IPlanService), DEPENDENCY INJECTION (IPlanRepository),
//           ABSTRACTION (only knows the repository interface, never EF Core), ENCAPSULATION (private helper),
//           DRY (FindOrThrowAsync + ApplyTo written once, used several times), change tracker updates,
//           typed exceptions (NotFoundException -> GlobalExceptionHandler -> 404), LINQ projection (Select)
// Related:  IPlanService, IPlanRepository, PlanMappings (ToDto out, ApplyTo in), Plan, NotFoundException,
//           PlansController (the caller)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;
using OneCover.Api.Errors;
using OneCover.Api.Models;
using OneCover.Api.Repositories;

namespace OneCover.Api.Services;

// plans = the injected repository. every DB call goes through it
public class PlanService(IPlanRepository plans) : IPlanService
{
    // load entities -> convert each one to a DTO.
    // .Select(p => p.ToDto()) = PROJECTION: "for each plan p, give me p.ToDto()". .ToList() collects the results
    public async Task<IReadOnlyList<PlanDto>> GetActiveAsync()
    {
        var active = await plans.GetActiveAsync();
        return active.Select(p => p.ToDto()).ToList();
    }

    // (await FindOrThrowAsync(id)) runs first, THEN .ToDto() is called on the plan it returns
    public async Task<PlanDto> GetByIdAsync(int id) => (await FindOrThrowAsync(id)).ToDto();

    // CREATE: new entity -> copy the request onto it -> mark for insert -> save -> return the DTO
    public async Task<PlanDto> CreateAsync(SavePlanRequest request)
    {
        var plan = new Plan();
        request.ApplyTo(plan);   // extension method from PlanMappings, copies every field

        plans.Add(plan);
        await plans.SaveChangesAsync(); // SQL Server assigns plan.Id *here*

        return plan.ToDto();
    }

    // UPDATE: load -> change -> save. no "update" call needed: EF's change tracker remembers the original
    // values from when it loaded the plan, compares on save, and sends an UPDATE for only the changed columns
    public async Task<PlanDto> UpdateAsync(int id, SavePlanRequest request)
    {
        var plan = await FindOrThrowAsync(id);
        request.ApplyTo(plan);
        await plans.SaveChangesAsync(); // the change tracker sees the edits and sends an UPDATE

        return plan.ToDto();
    }

    // RETIRE (soft delete): same load -> change -> save, just flipping IsActive.
    // the row stays in the DB b/c enrollments still point to it
    public async Task RetireAsync(int id)
    {
        var plan = await FindOrThrowAsync(id);
        plan.IsActive = false;
        await plans.SaveChangesAsync();
    }

    // shared by get, update and retire: load the plan or throw a 404
    // ?? throw = "use the plan, unless it's null, then throw". same fail-fast pattern as the JWT key in Program.cs
    // private = an internal detail of this class, not part of the IPlanService contract (encapsulation)
    private async Task<Plan> FindOrThrowAsync(int id) =>
            await plans.GetByIdAsync(id) ?? throw new NotFoundException($"Plan {id} not found.");
}
