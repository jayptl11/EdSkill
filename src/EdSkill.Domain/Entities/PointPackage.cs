namespace EdSkill.Domain.Entities;

public class PointPackage
{
    public Guid PointPackageId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Points { get; set; }
    public int BonusPoints { get; set; }
    public int PriceVnd { get; set; }
    public string Currency { get; set; } = "VND";
    public string? Description { get; set; }
    public string? BadgeText { get; set; }
    public bool IsHighlighted { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
