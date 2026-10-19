// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     unit tests for the Payment entity's rules: factories start valid, MarkPaid/MarkFailed only from Pending
// Layer:    Tests (xUnit)
// Concepts: testing ENCAPSULATION: Status/PaidDate have private setters, so the ONLY way to change them is
//           the methods under test. if these pass, no code anywhere can make an invalid payment
// Related:  Payment, PaymentStatus, PaymentType, BusinessRuleException, PaymentService (calls these)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Errors;
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Tests;

public class PaymentTests
{
    private static Payment NewPremium() =>
        Payment.PendingPremium(new Enrollment { Id = 1, UserId = 2 }, amount: 35m, dueDate: new DateOnly(2026, 11, 1));

    [Fact]
    public void PendingPremium_StartsPending_WithNoPaidDate()
    {
        var payment = NewPremium();

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(PaymentType.Premium, payment.Type);
        Assert.Null(payment.PaidDate);
        Assert.Equal(2, payment.UserId);
    }

    [Fact]
    public void MarkPaid_SetsStatusAndPaidDate_Together()
    {
        var payment = NewPremium();

        payment.MarkPaid(new DateOnly(2026, 11, 3));

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(new DateOnly(2026, 11, 3), payment.PaidDate);
    }

    [Fact]
    public void MarkPaid_Throws_WhenAlreadyPaid()
    {
        var payment = NewPremium();
        payment.MarkPaid(new DateOnly(2026, 11, 3));

        Assert.Throws<BusinessRuleException>(() => payment.MarkPaid(new DateOnly(2026, 11, 4)));
        Assert.Equal(new DateOnly(2026, 11, 3), payment.PaidDate);   // the original paid date is untouched
    }

    [Fact]
    public void MarkFailed_LeavesNoPaidDate_AndCantBePaidAfter()
    {
        var payment = NewPremium();

        payment.MarkFailed();

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Null(payment.PaidDate);
        Assert.Throws<BusinessRuleException>(() => payment.MarkPaid(new DateOnly(2026, 11, 3)));
    }
}
