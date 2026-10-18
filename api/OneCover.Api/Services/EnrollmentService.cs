// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     enrollment business logic: the rules for enrolling + creating the first premium payment
// Layer:    Services. controller above, 3 repositories below
// Concepts: DEPENDENCY INJECTION (5 deps), ABSTRACTION (interfaces only), business rules as typed exceptions,
//           UNIT OF WORK: the enrollment + payment are saved together in ONE SaveChangesAsync (one transaction),
//           so you can never end up w/ an enrollment that has no payment, or the reverse.
//           ICurrentUser for "who" (never the request), TimeProvider for "today" (testable)
// Related:  IEnrollmentService, IEnrollmentRepository, IPaymentRepository, IPlanRepository, ICurrentUser,
//           Payment.PendingPremium (B1), EnrollmentMappings, NotFoundException / BusinessRuleException
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;
using OneCover.Api.Errors;
using OneCover.Api.Models;
using OneCover.Api.Repositories;

namespace OneCover.Api.Services;

public class EnrollmentService(
    IEnrollmentRepository enrollments,
    IPaymentRepository payments,
    IPlanRepository plans,
    ICurrentUser currentUser,
    TimeProvider time) : IEnrollmentService
{
    public async Task<EnrollmentDto> EnrollAsync(EnrollRequest request)
    {
        var userId = currentUser.UserId;   // from the JWT, never from the request body

        // rule 1: the plan has to exist (404) and still be offered (400)
        var plan = await plans.GetByIdAsync(request.PlanId!.Value)
            ?? throw new NotFoundException($"Plan {request.PlanId} not found.");

        if (!plan.IsActive)
        {
            throw new BusinessRuleException("This plan is no longer offered.");
        }

        // rule 2: no duplicate coverage in the same plan
        if (await enrollments.HasActiveEnrollmentAsync(userId, plan.Id))
        {
            throw new BusinessRuleException("You're already enrolled in this plan.");
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        var enrollment = new Enrollment
        {
            UserId = userId,
            Plan = plan,          // navigation: EF fills in PlanId, and ToDto() can read the plan's name.
                                  // safe ONLY b/c plans.GetByIdAsync returns a TRACKED plan (see PlanRepository)
            EnrolledOn = today    // Status defaults to Active
        };

        // first premium is due on the 1st of next month. B1's factory builds it in a valid Pending state
        var firstDueDate = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        var premium = Payment.PendingPremium(enrollment, plan.MonthlyPremium, firstDueDate);

        enrollments.Add(enrollment);
        payments.Add(premium);

        // ONE save for both: the repositories share this request's DbContext, so EF inserts the enrollment,
        // gets its new Id, sets the payment's EnrollmentId, and inserts the payment, all in one transaction
        await enrollments.SaveChangesAsync();

        return enrollment.ToDto();
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetMineAsync()
    {
        var mine = await enrollments.GetByUserAsync(currentUser.UserId);
        return mine.Select(e => e.ToDto()).ToList();
    }
}
