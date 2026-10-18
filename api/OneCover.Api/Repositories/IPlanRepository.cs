// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     REPOSITORY INTERFACE = the contract for getting plans in + out of the database
// Layer:    Repositories (data access). PlanService calls this, it's the only path to plan data
// Concepts: ABSTRACTION (the service sees "get active plans", not SQL or EF), single responsibility,
//           ENCAPSULATION on return values (IReadOnlyList = callers can read, not add/remove),
//           YAGNI (no "get retired plans" method until something actually needs one)
// Related:  PlanRepository (EF Core implementation), PlanService (the caller, A3), Plan entity,
//           IUserRepository (same pattern), Program.cs (AddScoped<IPlanRepository, PlanRepository>)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;

namespace OneCover.Api.Repositories;

/// <summary>Data access for plans. Services use this instead of AppDbContext directly.</summary>
// no Update or Delete methods, on purpose:
//   update = GetByIdAsync -> change properties -> SaveChangesAsync (EF's change tracker sends the UPDATE)
//   retire = same thing, just setting IsActive = false. nothing is ever hard-deleted
public interface IPlanRepository
{
    /// <summary>Active plans only, sorted by name.</summary>
    Task<IReadOnlyList<Plan>> GetActiveAsync();

    /// <summary>Returns null if not found. Includes retired plans.</summary>
    Task<Plan?> GetByIdAsync(int id);

    // not a Task: only marks the plan for insert in EF's change tracker, no DB trip yet
    void Add(Plan plan);

    /// <summary>Sends everything added or changed to the database, in one transaction.</summary>
    Task SaveChangesAsync();
}
