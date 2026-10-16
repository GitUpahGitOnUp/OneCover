// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     enum for which way the money flows
// Layer:    Models (domain), stored as text in SQL Server
// Concepts: Type (direction) + Status (where it stands) together describe a payment
// Related:  Payment.Type, Payment.EnrollmentId (set for Premium) / Payment.ClaimId (set for Benefit)
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Models.Enums;

public enum PaymentType
{
    /// <summary>What the member pays for coverage.</summary>
    Premium,
    /// <summary>What the member receives from an approved claim.</summary>
    Benefit
}
