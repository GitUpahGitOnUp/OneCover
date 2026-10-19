// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     claim business logic. member side: ownership checks + filing (w/ the benefit calculated).
//           admin side: the paged queue, moving claims through the workflow, dashboard counts
// Layer:    Services. controllers above, repositories below
// Concepts: DEPENDENCY INJECTION (6 deps, all interfaces), ABSTRACTION, SECURITY (members only see/use their OWN
//           data; someone else's record = 404, not 403), ENCAPSULATION (the claim builds itself via Submit and
//           changes itself via MoveTo, the service just COORDINATES: load, call the entity, save),
//           UNIT OF WORK (approval saves the status change, history row and benefit payment in ONE transaction)
// Related:  IClaimService, IClaimRepository, IEnrollmentRepository, IUserRepository, IPaymentRepository,
//           IBenefitCalculator, ICurrentUser, BenefitClaim.Submit / MoveTo, Payment.PendingBenefit, ClaimMappings
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
    IUserRepository users,
    IPaymentRepository payments,
    IBenefitCalculator calculator,
    ICurrentUser currentUser,
    TimeProvider time) : IClaimService
{
    private const int MaxPageSize = 100;   // nobody can ask for 1,000,000 rows in one request

    // ── member actions ─────────────────────────────────────────────────────────────────────

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

        // the benefit math needs the member's salary (User) and the plan's rules (enrollment.Plan, Included)
        var member = await users.GetByIdAsync(enrollment.UserId)
            ?? throw new UnauthorizedException("Your account could not be found.");
        var plan = enrollment.Plan;

        var startDate = request.StartDate!.Value;
        var endDate = request.EndDate!.Value;

        // weekly: salary / 52 x percent, capped. total: daily rate x days after the waiting period.
        // non-disability plans have percent + max of 0, so they come out to 0
        var weekly = calculator.WeeklyBenefit(member.AnnualSalary, plan.BenefitPercent, plan.WeeklyMaximum);
        var total = calculator.TotalBenefit(weekly, startDate, endDate, plan.EliminationDays);

        // rule 3 (dates) lives INSIDE BenefitClaim.Submit: the claim protects itself
        var claim = BenefitClaim.Submit(
            enrollment,
            request.Reason,
            startDate,
            endDate,
            weeklyBenefit: weekly,
            totalBenefit: total,
            submittedAt: time.GetUtcNow().UtcDateTime);

        claims.Add(claim);
        await claims.SaveChangesAsync();   // inserts the claim, then its first history row

        // reload w/ member, plan + history user loaded, so ToDto() has every name it needs
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

        // members: only their own claims. admins: any claim.
        // "not yours" and "doesn't exist" look identical from the outside: 404
        if (claim is null || (claim.UserId != currentUser.UserId && !currentUser.IsAdmin))
        {
            throw new NotFoundException($"Claim {id} not found.");
        }

        return claim.ToDto();
    }

    // ── admin actions ──────────────────────────────────────────────────────────────────────

    public async Task<PagedResult<ClaimDto>> GetPageAsync(ClaimStatus? status, int page, int pageSize)
    {
        // Math.Clamp keeps the inputs sane: page at least 1, page size between 1 and 100
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, total) = await claims.GetPageAsync(status, page, pageSize);   // DECONSTRUCTS the tuple into 2 variables
        return new PagedResult<ClaimDto>(
            Items: items.Select(c => c.ToDto()).ToList(),
            Page: page,
            PageSize: pageSize,
            TotalCount: total);
    }

    // the plan's "load → MoveTo → save" recipe. the service never touches claim.Status itself:
    // MoveTo checks the workflow, writes the history row, and changes the status (encapsulation)
    public async Task<ClaimDto> UpdateStatusAsync(int id, UpdateClaimStatusRequest request)
    {
        var claim = await claims.GetByIdAsync(id)
            ?? throw new NotFoundException($"Claim {id} not found.");

        var now = time.GetUtcNow().UtcDateTime;

        // an invalid move throws BusinessRuleException here -> 400, and nothing below runs
        claim.MoveTo(request.Status!.Value, currentUser.UserId, request.Note, now);

        // approving a claim w/ money owed creates the Pending benefit payment, due today
        if (claim.Status == ClaimStatus.Approved && claim.TotalBenefit > 0)
        {
            payments.Add(Payment.PendingBenefit(claim, DateOnly.FromDateTime(now)));
        }

        // ONE save: the status UPDATE, the new history INSERT, and the payment INSERT, in one transaction
        await claims.SaveChangesAsync();

        // reload so the new history row's "changed by" name is loaded for the response
        var saved = await claims.GetByIdAsync(id)
            ?? throw new NotFoundException($"Claim {id} not found.");
        return saved.ToDto();
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var counts = await claims.CountByStatusAsync();

        // Enum.GetValues = every ClaimStatus, in order. GetValueOrDefault fills in 0 for statuses w/ no claims
        var byStatus = Enum.GetValues<ClaimStatus>()
            .Select(s => new StatusCountDto(Status: s, Count: counts.GetValueOrDefault(s)))
            .ToList();

        return new DashboardDto(TotalClaims: byStatus.Sum(x => x.Count), ClaimsByStatus: byStatus);
    }
}
