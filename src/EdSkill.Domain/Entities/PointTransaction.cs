using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class PointTransaction
{
    public Guid PointTransactionId { get; set; }
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }
    public Guid? SystemLedgerAccountId { get; set; }
    public virtual SystemLedgerAccount? SystemLedgerAccount { get; set; }
    public PointTransactionType Type { get; set; }
    public int Amount { get; set; }
    public int BalanceBefore { get; set; }
    public int BalanceAfter { get; set; }
    public int HeldBalanceBefore { get; set; }
    public int HeldBalanceAfter { get; set; }
    public Guid? SessionId { get; set; }
    public virtual Session? Session { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
