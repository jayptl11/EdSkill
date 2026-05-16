using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Achievements;

internal static class AchievementDtoMapper
{
    public static AchievementSummaryDto MapSummary(UserAchievement achievement)
    {
        return new AchievementSummaryDto(
            achievement.AchievementDefinitionId,
            achievement.AchievementDefinition.Name,
            achievement.AchievementDefinition.Description,
            achievement.AchievementDefinition.IconUrl,
            achievement.AwardedAt);
    }

    public static AdminAchievementDto MapAdmin(AchievementDefinition achievement)
    {
        return new AdminAchievementDto(
            achievement.AchievementDefinitionId,
            achievement.Name,
            achievement.Description,
            achievement.IconUrl,
            AchievementParsing.ToApiValue(achievement.Track),
            AchievementParsing.ToApiValue(achievement.Metric),
            achievement.Threshold,
            achievement.SortOrder,
            achievement.IsActive,
            achievement.EffectiveFromUtc);
    }
}
