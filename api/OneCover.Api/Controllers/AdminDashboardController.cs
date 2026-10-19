// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     CONTROLLER for the admin dashboard: claim counts by status
// Layer:    Controllers. one thin GET
// Concepts: [Authorize(Roles = "Admin")], DEPENDENCY INJECTION
// Related:  IClaimService.GetDashboardAsync, DashboardDto, Angular dashboard count cards (Day 8)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCover.Api.Dtos;
using OneCover.Api.Services;

namespace OneCover.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController(IClaimService claimService) : ControllerBase
{
    // GET /api/admin/dashboard -> 200 + { totalClaims, claimsByStatus: [ { status, count }, ... ] }
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get() => Ok(await claimService.GetDashboardAsync());
}
