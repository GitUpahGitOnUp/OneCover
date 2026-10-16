// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     enum for the kinds of coverage a plan can be
// Layer:    Models (domain), stored as text in SQL Server
// Concepts: XML doc comments (///) on values whose names aren't obvious, they show on hover anywhere
// Related:  Plan.Type, DbSeeder (seeds one plan of each type)
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Models.Enums;

public enum PlanType
{
    /// <summary>Short-term disability.</summary>
    STD,
    /// <summary>Long-term disability.</summary>
    LTD,
    Life,
    Accident,
    Dental
}
