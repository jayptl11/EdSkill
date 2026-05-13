using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class TokenTransaction
{
    public Guid TokenTransactionId { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public TokenTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public Guid? SessionId { get; set; }
    public virtual Session? Session { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
