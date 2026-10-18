// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the EF Core implementation of IPlanRepository. each method = one EF query or one save
// Layer:    Repositories (data access), the only layer that touches AppDbContext
// Concepts: implements an INTERFACE (: IPlanRepository), DEPENDENCY INJECTION (AppDbContext via the primary
//           constructor), LINQ + DEFERRED EXECUTION (a query is described step by step, then run once at the end),
//           EXTENSION METHODS (ToListAsync / FirstOrDefaultAsync come from Microsoft.EntityFrameworkCore,
//           same idea as our own plan.ToDto()), change tracker, scoped lifetime (matches the DbContext's)
// Related:  IPlanRepository, AppDbContext.Plans, Plan, PlanService (A3), UserRepository (same pattern)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Data;
using OneCover.Api.Models;
using Microsoft.EntityFrameworkCore;   // ToListAsync + FirstOrDefaultAsync live here (extension methods)

namespace OneCover.Api.Repositories;

// ": IPlanRepository" = promises every method the interface lists.
// trick: each method = the interface's line + "public" + a "=> body"
public class PlanRepository(AppDbContext db) : IPlanRepository
{
    // the query is BUILT step by step, then RUN once at ToListAsync (deferred execution):
    //   .AsNoTracking()          -> read-only: skip the change tracker (no snapshots to compare on save).
    //                               faster + less memory. only safe b/c nothing here gets edited and saved
    //   .Where(p => p.IsActive)  -> filter:  WHERE IsActive = 1
    //   .OrderBy(p => p.Name)    -> sort:    ORDER BY Name   (SQL has no guaranteed order w/o this)
    //   .ToListAsync()           -> RUN it, load the rows as Plan objects into a List
    // async/await needed here: ToListAsync returns Task<List<Plan>>, but we promised Task<IReadOnlyList<Plan>>.
    // C# won't convert one Task type to another, so await unwraps the List (which IS an IReadOnlyList)
    public async Task<IReadOnlyList<Plan>> GetActiveAsync() => await db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

    // SELECT TOP(1) ... WHERE Id = @id. null if no match. no async/await: FirstOrDefaultAsync already
    // returns exactly Task<Plan?>, so we just hand it back.
    // MUST STAY TRACKED (no AsNoTracking): PlanService's update/retire rely on the change tracker, and
    // EnrollmentService links a new enrollment w/ "Plan = plan". a detached plan there would get inserted as NEW
    public Task<Plan?> GetByIdAsync(int id) => db.Plans.FirstOrDefaultAsync(p => p.Id == id);

    // marks a new plan for INSERT in the change tracker. nothing goes to SQL Server yet
    public void Add(Plan plan) => db.Plans.Add(plan);

    // the one real DB trip: sends every pending insert/update (new plans, edited plans, retired plans) together
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
