using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class PaymentTransaction
{
    public Guid PaymentTransactionId { get; set; }
    public Guid UserId { get; set; }
    public virtual User? User { get; set; }
    public Guid? PointPackageId { get; set; }
    public virtual PointPackage? PointPackage { get; set; }
    public PaymentProvider Provider { get; set; } = PaymentProvider.VnPay;
    public string? ProviderTransactionId { get; set; }
    public int AmountVnd { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? PaymentUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RawPayload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
