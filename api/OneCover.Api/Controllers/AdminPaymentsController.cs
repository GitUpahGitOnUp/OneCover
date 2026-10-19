// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     CONTROLLER for the admin payments page: list payments + mark one Paid or Failed
// Layer:    Controllers. thin: the "only from Pending" rule lives in the Payment entity
// Concepts: [Authorize(Roles = "Admin")] on the class, [FromQuery] filter, route constraint {id:int}
// Related:  IPaymentService, PaymentDto, UpdatePaymentStatusRequest, Angular admin payments page (Day 8)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCover.Api.Dtos;
using OneCover.Api.Models.Enums;
using OneCover.Api.Services;

namespace OneCover.Api.Controllers;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController(IPaymentService paymentService) : ControllerBase
{
    // GET /api/admin/payments?status=Pending -> 200 + payments (all statuses if no filter)
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetAll([FromQuery] PaymentStatus? status) =>
        Ok(await paymentService.GetAllAsync(status));

    // PUT /api/admin/payments/5/status { "status": "Paid" } -> 200 + the updated payment
    // marking a non-Pending payment comes back as a 400 w/ the entity's message
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<PaymentDto>> UpdateStatus(int id, UpdatePaymentStatusRequest request) =>
        Ok(await paymentService.UpdateStatusAsync(id, request));
}
