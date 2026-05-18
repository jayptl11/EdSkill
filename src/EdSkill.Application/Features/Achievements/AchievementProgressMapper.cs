using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Achievements;

internal static class AchievementProgressMapper
{
    public static MyAchievementEarnedDto MapEarned(UserAchievement achievement)
    {
        return new MyAchievementEarnedDto(
            achievement.AchievementDefinitionId,
            achievement.AchievementDefinition.Name,
            achievement.AchievementDefinition.Description,
            achievement.AchievementDefinition.IconUrl,
            AchievementParsing.ToApiValue(achievement.AchievementDefinition.Track),
            AchievementParsing.ToApiValue(achievement.AchievementDefinition.Metric),
            achievement.AchievementDefinition.Threshold,
            achievement.AwardedAt);
    }

    public static MyUpcomingAchievementDto MapUpcoming(AchievementDefinition definition, int currentValue)
    {
        var threshold = definition.Threshold;
        var progressPercent = threshold <= 0
            ? 100d
            : Math.Round(Math.Min(100d, currentValue * 100d / threshold), 2);

        return new MyUpcomingAchievementDto(
            definition.AchievementDefinitionId,
            definition.Name,
            definition.Description,
            definition.IconUrl,
            AchievementParsing.ToApiValue(definition.Track),
            AchievementParsing.ToApiValue(definition.Metric),
            currentValue,
            threshold,
            Math.Max(0, threshold - currentValue),
            progressPercent);
    }
}
