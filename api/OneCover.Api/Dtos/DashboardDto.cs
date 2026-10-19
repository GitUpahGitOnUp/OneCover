// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for the admin dashboard: how many claims are in each status
// Layer:    Dtos, the API boundary
// Concepts: positional RECORDS, a DTO holding a list of small DTOs. every status is always listed (zeros included),
//           so the Angular count cards never have to guess about a missing status
// Related:  ClaimService.GetDashboardAsync, ClaimRepository.CountByStatusAsync (LINQ GroupBy),
//           AdminDashboardController, Angular dashboard count cards (Day 8)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

/// <summary>Claim counts by status for the admin dashboard.</summary>
public record DashboardDto(int TotalClaims, IReadOnlyList<StatusCountDto> ClaimsByStatus);

/// <summary>How many claims are in one status.</summary>
public record StatusCountDto(ClaimStatus Status, int Count);
