namespace EdSkill.Domain.Entities;

public class PointWallet
{
    public Guid PointWalletId { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public int Balance { get; set; }
    public int HeldBalance { get; set; }
    public int TotalEarned { get; set; }
    public int TotalSpent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
