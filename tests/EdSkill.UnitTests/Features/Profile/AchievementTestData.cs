using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;

namespace EdSkill.UnitTests.Features.Profile;

internal static class AchievementTestData
{
    public static UserAchievement CreateUserAchievement(Guid userId, Guid achievementId, string name = "First Session")
    {
        return new UserAchievement
        {
            UserAchievementId = Guid.NewGuid(),
            UserId = userId,
            AchievementDefinitionId = achievementId,
            AwardedAt = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc),
            AchievementDefinition = new AchievementDefinition
            {
                AchievementDefinitionId = achievementId,
                Name = name,
                Description = "Achievement description",
                Track = AchievementTrack.Companion,
                Metric = AchievementMetric.CompletedSessions,
                Threshold = 1,
                SortOrder = 1,
                IsActive = true,
                EffectiveFromUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };
    }
}
