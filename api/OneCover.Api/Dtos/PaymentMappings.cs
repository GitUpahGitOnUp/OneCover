// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     Payment entity -> PaymentDto, including a human-readable Description
// Layer:    Dtos (mapping)
// Concepts: EXTENSION METHOD, named arguments, the null-conditional operator (?.) for the OPTIONAL navigations
//           (a premium has an Enrollment but no Claim, a benefit has a Claim but no Enrollment), switch expression
// Related:  Payment, PaymentDto, PaymentRepository (Includes everything this reads), PaymentService
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

public static class PaymentMappings
{
    /// <summary>Converts a Payment (with its User, Enrollment.Plan / Claim.Enrollment.Plan loaded) to the DTO the API returns.</summary>
    public static PaymentDto ToDto(this Payment payment) => new(
        Id: payment.Id,
        MemberName: $"{payment.User.FirstName} {payment.User.LastName}",
        Type: payment.Type,
        Description: Describe(payment),
        Amount: payment.Amount,
        DueDate: payment.DueDate,
        PaidDate: payment.PaidDate,
        Status: payment.Status);

    // "Short-Term Disability premium" or "Claim #3 benefit (Short-Term Disability)".
    // ?. = "only keep going if this isn't null", ?? = "if it was null, use this instead"
    private static string Describe(Payment payment) => payment.Type switch
    {
        PaymentType.Premium => $"{payment.Enrollment?.Plan.Name ?? "Coverage"} premium",
        PaymentType.Benefit => $"Claim #{payment.ClaimId} benefit ({payment.Claim?.Enrollment.Plan.Name ?? "coverage"})",
        _ => payment.Type.ToString()
    };
}
