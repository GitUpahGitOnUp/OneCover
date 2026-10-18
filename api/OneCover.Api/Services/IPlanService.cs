// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     SERVICE INTERFACE for plans: the contract for everything the API can do w/ plans
// Layer:    Services (business logic). PlansController calls this, it calls IPlanRepository
// Concepts: ABSTRACTION (the controller only knows this contract, not how it works),
//           DTOs in + out (SavePlanRequest in, PlanDto out, entities never leave the service),
//           async (every method returns a Task b/c each one hits the database)
// Related:  PlanService (implementation), PlansController (caller), PlanDto / SavePlanRequest,
//           NotFoundException (thrown when an id doesn't exist), Program.cs (AddScoped<IPlanService, PlanService>)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;

namespace OneCover.Api.Services;

/// <summary> Business Logic for coverage plans. Returns DTOs, never entities. </summary>
public interface IPlanService
{
    /// <summary> Active plans *ONLY*, sorted by name. </summary>
    Task<IReadOnlyList<PlanDto>> GetActiveAsync();

    // one parameter = part of the method's SIGNATURE. GetByIdAsync() and GetByIdAsync(int) would be two
    // different methods (overloading), which is why the extra one broke PlanService
    /// <summary>Throws NotFoundException if no plan has this id.</summary>
    Task<PlanDto> GetByIdAsync(int id);

    Task<PlanDto> CreateAsync(SavePlanRequest request);

    /// <summary>Throws NotFoundException if no plan has this id.</summary>
    Task<PlanDto> UpdateAsync(int id, SavePlanRequest request);

    // plain Task (no <T>) = nothing to return, the caller just waits for it to finish
    /// <summary>Soft delete: sets IsActive = false. Throws NotFoundException if no plan has this id.</summary>
    Task RetireAsync(int id);
}
