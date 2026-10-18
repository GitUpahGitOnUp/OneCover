// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     CONTROLLER for plans: maps the 5 plan endpoints (method + URL) to IPlanService calls
// Layer:    Controllers, the top layer. THIN on purpose: no business rules, no try/catch
//           (a NotFoundException from the service bubbles up to GlobalExceptionHandler -> 404)
// Concepts: INHERITANCE (: ControllerBase), DEPENDENCY INJECTION (IPlanService), ABSTRACTION,
//           attribute routing + route constraints ({id:int}), role-based authorization (Admin-only writes),
//           REST status codes: 200 OK, 201 Created, 204 No Content (+ 400/401/403/404 from the framework/handler)
// Related:  IPlanService / PlanService, PlanDto, SavePlanRequest, Program.cs (MapControllers finds this)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCover.Api.Dtos;
using OneCover.Api.Services;

namespace OneCover.Api.Controllers;

[ApiController]          // auto-validates request DTOs (400 before the method runs) + reads them from the JSON body
[Route("api/plans")]     // base URL for every action below
public class PlansController(IPlanService planService) : ControllerBase
{
    // GET /api/plans -> 200 + the list of active plans. no [Authorize]: anyone can browse coverage
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> GetActive() =>
                Ok(await planService.GetActiveAsync());

    // GET /api/plans/5 -> 200 + one plan (404 if not found)
    // {id:int} = route parameter + constraint: fills the 'id' parameter from the URL, and only matches whole
    // numbers (/api/plans/abc doesn't match at all)
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlanDto>> GetById(int id) =>
            Ok(await planService.GetByIdAsync(id));

    // POST /api/plans -> 201 Created (admins only, a member gets 403)
    // CreatedAtAction = 201 + the new plan in the body + a Location header pointing to GET /api/plans/{new id}.
    // nameof(GetById) = "the URL of the GetById action", w/o typing the method name as a fragile string
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<PlanDto>> Create(SavePlanRequest request)
    {
        var plan = await planService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id}, plan);
    }

    // PUT /api/plans/5 -> 200 + the updated plan. id from the URL, new values from the JSON body
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlanDto>> Update(int id, SavePlanRequest request) =>
            Ok(await planService.UpdateAsync(id, request));

    // DELETE /api/plans/5 -> 204 No Content. it's a SOFT delete (retire): the plan stays in the DB w/ IsActive = false
    // IActionResult (no <T>) b/c there's no body to describe
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Retire(int id)
    {
        await planService.RetireAsync(id);
        return NoContent();
    }
}
