// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     CONTROLLER for "who am I": the logged-in user's profile + FMLA eligibility
// Layer:    Controllers. thin
// Concepts: [Authorize] (any logged-in user). the URL has no id on purpose: /api/me is always YOU,
//           taken from the JWT, so there's nothing to tamper with
// Related:  IProfileService, ProfileDto, Angular profile area (Day 7)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCover.Api.Dtos;
using OneCover.Api.Services;

namespace OneCover.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(IProfileService profileService) : ControllerBase
{
    // GET /api/me -> 200 + the logged-in user's profile
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get() => Ok(await profileService.GetMineAsync());
}
