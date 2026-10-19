// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     payment business logic: member statements, the admin list, marking payments Paid/Failed
// Layer:    Services. controllers above, IPaymentRepository below
// Concepts: ENCAPSULATION: the service never sets payment.Status. it calls MarkPaid / MarkFailed, and the
//           Payment entity enforces "only from Pending" and "Paid always has a PaidDate".
//           switch statement on the requested status, ICurrentUser for "who", TimeProvider for "today"
// Related:  IPaymentService, IPaymentRepository, Payment.MarkPaid / MarkFailed, PaymentMappings
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;
using OneCover.Api.Errors;
using OneCover.Api.Models.Enums;
using OneCover.Api.Repositories;

namespace OneCover.Api.Services;

public class PaymentService(
    IPaymentRepository payments,
    ICurrentUser currentUser,
    TimeProvider time) : IPaymentService
{
    public async Task<IReadOnlyList<PaymentDto>> GetMineAsync()
    {
        var mine = await payments.GetByUserAsync(currentUser.UserId);
        return mine.Select(p => p.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetAllAsync(PaymentStatus? status)
    {
        var all = await payments.GetAllAsync(status);
        return all.Select(p => p.ToDto()).ToList();
    }

    public async Task<PaymentDto> UpdateStatusAsync(int id, UpdatePaymentStatusRequest request)
    {
        var payment = await payments.GetByIdAsync(id)
            ?? throw new NotFoundException($"Payment {id} not found.");

        // the entity's methods do the real work + enforce the rules (throw if it isn't Pending)
        switch (request.Status)
        {
            case PaymentStatus.Paid:
                payment.MarkPaid(request.PaidOn ?? DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime));
                break;
            case PaymentStatus.Failed:
                payment.MarkFailed();
                break;
            default:
                throw new BusinessRuleException("A payment can only be marked Paid or Failed.");
        }

        await payments.SaveChangesAsync();   // change tracker sends one UPDATE w/ the new Status (+ PaidDate)
        return payment.ToDto();
    }
}
