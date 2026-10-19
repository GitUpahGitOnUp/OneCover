// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     CONTROLLER for the admin claims queue: list (paged, filterable) + move a claim to a new status
// Layer:    Controllers. thin: the workflow rules live in ClaimWorkflow / BenefitClaim.MoveTo
// Concepts: [Authorize(Roles = "Admin")] on the class = every action is admin-only (members get 403),
//           [FromQuery] = values read from the URL's query string (?status=InReview&page=2&pageSize=20),
//           optional parameters w/ defaults
// Related:  IClaimService, PagedResult<ClaimDto>, UpdateClaimStatusRequest, Angular admin claims queue (Day 8)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCover.Api.Dtos;
using OneCover.Api.Models.Enums;
using OneCover.Api.Services;

namespace OneCover.Api.Controllers;

[ApiController]
[Route("api/admin/claims")]
[Authorize(Roles = "Admin")]
public class AdminClaimsController(IClaimService claimService) : ControllerBase
{
    // GET /api/admin/claims?status=InReview&page=1&pageSize=20 -> 200 + one page of claims
    // every query value is optional: no status = all statuses, defaults = page 1 of 20
    [HttpGet]
    public async Task<ActionResult<PagedResult<ClaimDto>>> GetPage(
        [FromQuery] ClaimStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        Ok(await claimService.GetPageAsync(status, page, pageSize));

    // PUT /api/admin/claims/5/status { "status": "InReview", "note": "Starting review" } -> 200 + the updated claim.
    // an invalid move (e.g. Submitted -> Approved) comes back as a 400 w/ the workflow's message
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ClaimDto>> UpdateStatus(int id, UpdateClaimStatusRequest request) =>
        Ok(await claimService.UpdateStatusAsync(id, request));
}
