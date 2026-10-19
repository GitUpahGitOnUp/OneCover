// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     CONTROLLER for a member's statement: their premium + benefit payments
// Layer:    Controllers. thin
// Concepts: [Authorize(Roles = "Member")], DEPENDENCY INJECTION. "whose" statement comes from the JWT, not the URL
// Related:  IPaymentService.GetMineAsync, PaymentDto, Angular statements page (Day 7)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCover.Api.Dtos;
using OneCover.Api.Services;

namespace OneCover.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Member")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    // GET /api/payments -> 200 + the logged-in member's payments
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetMine() =>
        Ok(await paymentService.GetMineAsync());
}
