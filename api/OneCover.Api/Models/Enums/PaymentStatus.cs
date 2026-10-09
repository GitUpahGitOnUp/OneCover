namespace OneCover.Api.Models.Enums;

/// <summary> for any payment type i.e. emp -> insurer (paying premiums) or employer -> employee (benefit payments) </summary>
public enum PaymentStatus
{
    Pending,
    Paid,
    Failed
}
