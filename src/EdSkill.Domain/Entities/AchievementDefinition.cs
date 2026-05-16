using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class AchievementDefinition
{
    public Guid AchievementDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public AchievementTrack Track { get; set; }
    public AchievementMetric Metric { get; set; }
    public int Threshold { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
