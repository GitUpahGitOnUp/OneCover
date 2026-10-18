// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     claim business logic: ownership checks, filing through BenefitClaim.Submit, reading claims
// Layer:    Services. controller above, repositories below
// Concepts: DEPENDENCY INJECTION, ABSTRACTION, SECURITY (members only ever see/use their OWN data, checked
//           against the JWT's user id; someone else's record = 404, not 403, so its existence isn't revealed),
//           ENCAPSULATION (the claim builds itself via Submit, the service just coordinates)
// Related:  IClaimService, IClaimRepository, IEnrollmentRepository, ICurrentUser, BenefitClaim.Submit (C1),
//           ClaimMappings, NotFoundException / BusinessRuleException, BenefitCalculator (Day 5, real amounts)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;
using OneCover.Api.Errors;
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;
using OneCover.Api.Repositories;

namespace OneCover.Api.Services;

public class ClaimService(
    IClaimRepository claims,
    IEnrollmentRepository enrollments,
    ICurrentUser currentUser,
    TimeProvider time) : IClaimService
{
    public async Task<ClaimDto> SubmitAsync(SubmitClaimRequest request)
    {
        // rule 1: the enrollment must exist AND belong to this member. both cases get the same 404,
        // so a member can't probe for other people's enrollment ids
        var enrollment = await enrollments.GetByIdAsync(request.EnrollmentId!.Value);
        if (enrollment is null || enrollment.UserId != currentUser.UserId)
        {
            throw new NotFoundException($"Enrollment {request.EnrollmentId} not found.");
        }

        // rule 2: only active coverage can be claimed against
        if (enrollment.Status != EnrollmentStatus.Active)
        {
            throw new BusinessRuleException("This coverage is no longer active.");
        }

        // rule 3 (end date on or after start date) lives INSIDE BenefitClaim.Submit, the claim protects itself.
        // TODO Day 5: replace the two 0m amounts w/ the BenefitCalculator's weekly + total benefit
        var claim = BenefitClaim.Submit(
            enrollment,
            request.Reason,
            request.StartDate!.Value,
            request.EndDate!.Value,
            weeklyBenefit: 0m,
            totalBenefit: 0m,
            submittedAt: time.GetUtcNow().UtcDateTime);

        claims.Add(claim);
        await claims.SaveChangesAsync();   // inserts the claim, then its first history row

        // reload w/ the plan + history user loaded, so ToDto() has the plan name and "changed by" name
        var saved = await claims.GetByIdAsync(claim.Id)
            ?? throw new NotFoundException($"Claim {claim.Id} not found.");
        return saved.ToDto();
    }

    public async Task<IReadOnlyList<ClaimDto>> GetMineAsync()
    {
        var mine = await claims.GetByUserAsync(currentUser.UserId);
        return mine.Select(c => c.ToDto()).ToList();
    }

    public async Task<ClaimDto> GetByIdAsync(int id)
    {
        var claim = await claims.GetByIdAsync(id);

        // members: only their own claims. admins: any claim (they review them on Day 8).
        // "not yours" and "doesn't exist" look identical from the outside: 404
        if (claim is null || (claim.UserId != currentUser.UserId && !currentUser.IsAdmin))
        {
            throw new NotFoundException($"Claim {id} not found.");
        }

        return claim.ToDto();
    }
}
