using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Achievements.Queries.GetMyAchievements;

public class GetMyAchievementsQueryHandler : IRequestHandler<GetMyAchievementsQuery, Result<MyAchievementsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyAchievementsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<MyAchievementsDto>> Handle(GetMyAchievementsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();

        var earnedAchievements = await _context.UserAchievements
            .AsNoTracking()
            .Include(item => item.AchievementDefinition)
            .Where(item => item.UserId == userId && item.AchievementDefinition.IsActive)
            .OrderBy(item => item.AchievementDefinition.SortOrder)
            .ThenBy(item => item.AwardedAt)
            .ToListAsync(cancellationToken);

        var earnedDefinitionIds = earnedAchievements
            .Select(item => item.AchievementDefinitionId)
            .ToHashSet();

        var activeDefinitions = await _context.AchievementDefinitions
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var upcomingDefinitions = activeDefinitions
            .Where(item => !earnedDefinitionIds.Contains(item.AchievementDefinitionId))
            .ToList();

        var upcoming = new List<MyUpcomingAchievementDto>(upcomingDefinitions.Count);
        foreach (var definition in upcomingDefinitions)
        {
            var currentValue = await ResolveMetricValueAsync(userId, definition, cancellationToken);
            upcoming.Add(AchievementProgressMapper.MapUpcoming(definition, currentValue));
        }

        return Result<MyAchievementsDto>.Success(new MyAchievementsDto(
            earnedAchievements.Select(AchievementProgressMapper.MapEarned).ToList(),
            upcoming));
    }

    private Task<int> ResolveMetricValueAsync(Guid userId, AchievementDefinition definition, CancellationToken cancellationToken)
    {
        var completedSessions = _context.Sessions
            .AsNoTracking()
            .Where(item =>
                item.Status == SessionStatus.Completed
                && item.DisbursedAt.HasValue
                && item.DisbursedAt.Value >= definition.EffectiveFromUtc);

        return definition.Metric switch
        {
            AchievementMetric.CompletedSessions => definition.Track == AchievementTrack.Companion
                ? completedSessions.CountAsync(item => item.CompanionId == userId, cancellationToken)
                : completedSessions.CountAsync(item => item.LearnerId == userId, cancellationToken),
            AchievementMetric.CompletedHours => CountCompletedHoursAsync(completedSessions, userId, definition.Track, cancellationToken),
            AchievementMetric.DistinctCompletedLearners => CountDistinctCompletedLearnersAsync(completedSessions, userId, definition.Track, cancellationToken),
            _ => Task.FromResult(0)
        };
    }

    private static async Task<int> CountCompletedHoursAsync(
        IQueryable<Session> completedSessions,
        Guid userId,
        AchievementTrack track,
        CancellationToken cancellationToken)
    {
        var totalMinutes = await (track == AchievementTrack.Companion
                ? completedSessions.Where(item => item.CompanionId == userId)
                : completedSessions.Where(item => item.LearnerId == userId))
            .SumAsync(item => item.ActualDuration ?? 0, cancellationToken);

        return totalMinutes / 60;
    }

    private static Task<int> CountDistinctCompletedLearnersAsync(
        IQueryable<Session> completedSessions,
        Guid userId,
        AchievementTrack track,
        CancellationToken cancellationToken)
    {
        if (track != AchievementTrack.Companion)
        {
            return Task.FromResult(0);
        }

        return completedSessions
            .Where(item => item.CompanionId == userId && item.LearnerId.HasValue)
            .Select(item => item.LearnerId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
    }
}
