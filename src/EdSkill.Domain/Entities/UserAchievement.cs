namespace EdSkill.Domain.Entities;

public class UserAchievement
{
    public Guid UserAchievementId { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public Guid AchievementDefinitionId { get; set; }
    public virtual AchievementDefinition AchievementDefinition { get; set; } = null!;
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
