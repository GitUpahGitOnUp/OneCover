// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     CONTROLLER for enrollments: GET my coverage, POST enroll
// Layer:    Controllers. thin: HTTP only, the rules live in EnrollmentService
// Concepts: class-level [Authorize(Roles = "Member")] applies to EVERY action (members only, admins get 403),
//           DEPENDENCY INJECTION, 201 Created w/o a Location header (there's no GET-by-id endpoint to point to)
// Related:  IEnrollmentService, EnrollRequest, EnrollmentDto, Program.cs
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCover.Api.Dtos;
using OneCover.Api.Services;

namespace OneCover.Api.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize(Roles = "Member")]   // on the CLASS = applies to every action below
public class EnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    // GET /api/enrollments -> 200 + the logged-in member's enrollments
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EnrollmentDto>>> GetMine() =>
        Ok(await enrollmentService.GetMineAsync());

    // POST /api/enrollments { "planId": 1 } -> 201 + the new enrollment.
    // StatusCode(201, ...) instead of CreatedAtAction b/c there's no "get one enrollment" endpoint to link to
    [HttpPost]
    public async Task<ActionResult<EnrollmentDto>> Enroll(EnrollRequest request) =>
        StatusCode(StatusCodes.Status201Created, await enrollmentService.EnrollAsync(request));
}
