// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     enum for where a payment stands. shared by both payment directions (premiums + benefits)
// Layer:    Models (domain), stored as text in SQL Server
// Concepts: one shared enum b/c both directions have the same lifecycle (Pending -> Paid or Failed)
// Related:  Payment.Status (private set, only changes via MarkPaid/MarkFailed on Day 5), PaymentType
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Models.Enums;

/// <summary> for any payment type i.e. emp -> insurer (paying premiums) or employer -> employee (benefit payments) </summary>
public enum PaymentStatus
{
    Pending,
    Paid,
    Failed
}
