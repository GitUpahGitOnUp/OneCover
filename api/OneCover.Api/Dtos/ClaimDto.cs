// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTOs for a claim going OUT: the claim itself + its status history timeline
// Layer:    Dtos, the API boundary
// Concepts: positional RECORDS, a DTO containing a list of other DTOs (History), FLATTENED fields
//           (PlanName instead of a nested plan, ChangedBy as a display name instead of a nested user)
// Related:  BenefitClaim + ClaimStatusHistory (built from), ClaimMappings, ClaimService, ClaimsController,
//           Angular "My Claims" table + claim detail timeline (Day 7), admin claim review (Day 8)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

/// <summary>A claim as the API returns it, including its full status history (oldest first).</summary>
public record ClaimDto(
    int Id,
    int EnrollmentId,
    string PlanName,
    PlanType PlanType,
    string Reason,
    DateOnly StartDate,
    DateOnly EndDate,
    ClaimStatus Status,
    decimal WeeklyBenefit,
    decimal TotalBenefit,
    DateTime SubmittedAt,
    IReadOnlyList<ClaimHistoryDto> History);

/// <summary>One status change in a claim's timeline. FromStatus is null on the first (submission) row.</summary>
public record ClaimHistoryDto(
    ClaimStatus? FromStatus,
    ClaimStatus ToStatus,
    string ChangedBy,       // display name ("Barbara Barbarosa"), never the user's id or email
    DateTime ChangedAt,
    string? Note);
