using EdSkill.Application.Common.Interfaces;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Common.Services;

public class AchievementAwardService : IAchievementAwardService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AchievementAwardService(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task AwardForCompletedSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        var awardedAt = session.DisbursedAt ?? _dateTimeProvider.UtcNow;
        var definitions = await _context.AchievementDefinitions
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        if (definitions.Count == 0)
        {
            return;
        }

        if (session.LearnerId.HasValue)
        {
            await AwardForUserAsync(session, session.LearnerId.Value, AchievementTrack.Learner, awardedAt, definitions, cancellationToken);
        }

        await AwardForUserAsync(session, session.CompanionId, AchievementTrack.Companion, awardedAt, definitions, cancellationToken);
    }

    private async Task AwardForUserAsync(
        Session session,
        Guid userId,
        AchievementTrack track,
        DateTime awardedAt,
        IReadOnlyCollection<AchievementDefinition> definitions,
        CancellationToken cancellationToken)
    {
        var scopedDefinitions = definitions.Where(item => item.Track == track).ToList();
        if (scopedDefinitions.Count == 0)
        {
            return;
        }

        var existingAchievementIds = await _context.UserAchievements
            .Where(item => item.UserId == userId)
            .Select(item => item.AchievementDefinitionId)
            .ToListAsync(cancellationToken);

        foreach (var definition in scopedDefinitions)
        {
            if (existingAchievementIds.Contains(definition.AchievementDefinitionId))
            {
                continue;
            }

            if (definition.Metric == AchievementMetric.DistinctCompletedLearners && track != AchievementTrack.Companion)
            {
                continue;
            }

            var value = await ResolveMetricValueAsync(session, userId, track, definition, cancellationToken);
            if (value < definition.Threshold)
            {
                continue;
            }

            await _context.UserAchievements.AddAsync(new UserAchievement
            {
                UserAchievementId = Guid.NewGuid(),
                UserId = userId,
                AchievementDefinitionId = definition.AchievementDefinitionId,
                AwardedAt = awardedAt,
                CreatedAt = awardedAt
            }, cancellationToken);
        }
    }

    private Task<int> ResolveMetricValueAsync(
        Session session,
        Guid userId,
        AchievementTrack track,
        AchievementDefinition definition,
        CancellationToken cancellationToken)
    {
        var completedSessions = _context.Sessions
            .Where(item =>
                item.Status == SessionStatus.Completed
                && item.DisbursedAt.HasValue
                && item.DisbursedAt.Value >= definition.EffectiveFromUtc);

        return definition.Metric switch
        {
            AchievementMetric.CompletedSessions => CountCompletedSessionsAsync(completedSessions, userId, track, cancellationToken),
            AchievementMetric.CompletedHours => CountCompletedHoursAsync(completedSessions, userId, track, cancellationToken),
            AchievementMetric.DistinctCompletedLearners => CountDistinctCompletedLearnersAsync(completedSessions, userId, cancellationToken),
            _ => Task.FromResult(0)
        };
    }

    private static Task<int> CountCompletedSessionsAsync(
        IQueryable<Session> query,
        Guid userId,
        AchievementTrack track,
        CancellationToken cancellationToken)
    {
        return track == AchievementTrack.Companion
            ? query.CountAsync(item => item.CompanionId == userId, cancellationToken)
            : query.CountAsync(item => item.LearnerId == userId, cancellationToken);
    }

    private static async Task<int> CountCompletedHoursAsync(
        IQueryable<Session> query,
        Guid userId,
        AchievementTrack track,
        CancellationToken cancellationToken)
    {
        var minutes = await (track == AchievementTrack.Companion
                ? query.Where(item => item.CompanionId == userId)
                : query.Where(item => item.LearnerId == userId))
            .SumAsync(item => item.ActualDuration ?? 0, cancellationToken);

        return minutes / 60;
    }

    private static Task<int> CountDistinctCompletedLearnersAsync(
        IQueryable<Session> query,
        Guid companionId,
        CancellationToken cancellationToken)
    {
        return query
            .Where(item => item.CompanionId == companionId && item.LearnerId.HasValue)
            .Select(item => item.LearnerId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
    }
}
