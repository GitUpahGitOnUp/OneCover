// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     CONTROLLER for claims: list mine, get one, file a claim
// Layer:    Controllers. thin: HTTP only, ownership + rules live in ClaimService and BenefitClaim
// Concepts: [Authorize] on the class (must be logged in) + tighter [Authorize(Roles = "Member")] on member-only actions,
//           DEPENDENCY INJECTION, 201 Created w/ a Location header (CreatedAtAction -> GET /api/claims/{id})
// Related:  IClaimService, SubmitClaimRequest, ClaimDto, Program.cs
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCover.Api.Dtos;
using OneCover.Api.Services;

namespace OneCover.Api.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize]   // every action needs a logged-in user; some add a role requirement on top
public class ClaimsController(IClaimService claimService) : ControllerBase
{
    // GET /api/claims -> 200 + the member's own claims
    [Authorize(Roles = "Member")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClaimDto>>> GetMine() =>
        Ok(await claimService.GetMineAsync());

    // GET /api/claims/5 -> 200 + one claim. members get their own only (404 otherwise), admins can see any
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClaimDto>> GetById(int id) =>
        Ok(await claimService.GetByIdAsync(id));

    // POST /api/claims -> 201 Created + the new claim, w/ a Location header pointing to GET /api/claims/{id}
    [Authorize(Roles = "Member")]
    [HttpPost]
    public async Task<ActionResult<ClaimDto>> Submit(SubmitClaimRequest request)
    {
        var claim = await claimService.SubmitAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
    }
}
