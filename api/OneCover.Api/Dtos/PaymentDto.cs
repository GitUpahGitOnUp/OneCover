// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for a payment going OUT: one line on a member's statement or the admin payments page
// Layer:    Dtos, the API boundary
// Concepts: positional RECORD, FLATTENED: a ready-to-show Description ("Short-Term Disability premium") instead of
//           making the front end dig through the enrollment/claim/plan itself
// Related:  Payment (built from), PaymentMappings, PaymentService, PaymentsController + AdminPaymentsController,
//           Angular statements page (Day 7), admin payments page (Day 8)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

/// <summary>A premium or benefit payment as the API returns it. PaidDate is null unless Status is Paid.</summary>
public record PaymentDto(
    int Id,
    string MemberName,
    PaymentType Type,
    string Description,
    decimal Amount,
    DateOnly DueDate,
    DateOnly? PaidDate,
    PaymentStatus Status);
