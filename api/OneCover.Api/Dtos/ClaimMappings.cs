// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     BenefitClaim entity -> ClaimDto (and each history row -> ClaimHistoryDto)
// Layer:    Dtos (mapping)
// Concepts: EXTENSION METHODS, named arguments, navigation properties (claim.User, claim.Enrollment.Plan,
//           history.ChangedByUser), so the repository must Include them or they'd be null.
//           NextStatuses comes from ClaimWorkflow, so the UI can only offer moves the server would accept
// Related:  BenefitClaim, ClaimStatusHistory, ClaimDto, ClaimRepository (the Includes), ClaimService
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;

namespace OneCover.Api.Dtos;

public static class ClaimMappings
{
    /// <summary>Converts a claim (with Enrollment.Plan and History.ChangedByUser loaded) to the DTO the API returns.</summary>
    public static ClaimDto ToDto(this BenefitClaim claim) => new(
        Id: claim.Id,
        EnrollmentId: claim.EnrollmentId,
        MemberName: $"{claim.User.FirstName} {claim.User.LastName}",
        PlanName: claim.Enrollment.Plan.Name,          // two navigations deep: claim -> enrollment -> plan
        PlanType: claim.Enrollment.Plan.Type,
        Reason: claim.Reason,
        StartDate: claim.StartDate,
        EndDate: claim.EndDate,
        Status: claim.Status,
        WeeklyBenefit: claim.WeeklyBenefit,
        TotalBenefit: claim.TotalBenefit,
        SubmittedAt: claim.SubmittedAt,
        NextStatuses: ClaimWorkflow.NextStatuses(claim.Status),   // the rulebook decides, not the front end
        History: claim.History
            .OrderBy(h => h.ChangedAt)                  // timeline order: oldest first...
            .ThenBy(h => h.Id)                          // ...and ids break ties (same timestamp = creation order)
            .Select(h => h.ToDto())
            .ToList());

    /// <summary>Converts one history row (with ChangedByUser loaded) to its DTO.</summary>
    public static ClaimHistoryDto ToDto(this ClaimStatusHistory history) => new(
        FromStatus: history.FromStatus,
        ToStatus: history.ToStatus,
        ChangedBy: $"{history.ChangedByUser.FirstName} {history.ChangedByUser.LastName}",
        ChangedAt: history.ChangedAt,
        Note: history.Note);
}
