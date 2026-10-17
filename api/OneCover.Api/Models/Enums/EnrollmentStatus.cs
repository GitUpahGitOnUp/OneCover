// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     enum for whether a member's coverage is still in effect
// Layer:    Models (domain), stored as text in SQL Server
// Concepts: type safety, its own enum (not reusing ClaimStatus) so the compiler stops mix-ups
// Related:  Enrollment.Status (defaults to Active)
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Models.Enums;

public enum EnrollmentStatus
{
    Active,
    Cancelled
}
