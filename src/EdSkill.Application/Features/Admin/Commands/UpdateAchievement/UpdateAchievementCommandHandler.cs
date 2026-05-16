using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements;
using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Commands.UpdateAchievement;

public class UpdateAchievementCommandHandler : IRequestHandler<UpdateAchievementCommand, Result<AdminAchievementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IObjectStorageService _objectStorageService;

    public UpdateAchievementCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IObjectStorageService objectStorageService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _objectStorageService = objectStorageService;
    }

    public async Task<Result<AdminAchievementDto>> Handle(UpdateAchievementCommand request, CancellationToken cancellationToken)
    {
        var achievement = await _context.AchievementDefinitions
            .FirstOrDefaultAsync(item => item.AchievementDefinitionId == request.AchievementId, cancellationToken);
        if (achievement == null)
        {
            return Result<AdminAchievementDto>.Failure("ACHIEVEMENT_NOT_FOUND", "Achievement was not found.");
        }

        var newTrack = achievement.Track;
        if (request.HasTrack)
        {
            if (!AchievementParsing.TryParseTrack(request.Track, out newTrack))
            {
                return Result<AdminAchievementDto>.Failure("INVALID_ACHIEVEMENT_TRACK", "Achievement track is invalid.");
            }
        }

        var newMetric = achievement.Metric;
        if (request.HasMetric)
        {
            if (!AchievementParsing.TryParseMetric(request.Metric, out newMetric))
            {
                return Result<AdminAchievementDto>.Failure("INVALID_ACHIEVEMENT_METRIC", "Achievement metric is invalid.");
            }
        }

        var newThreshold = request.HasThreshold ? request.Threshold ?? achievement.Threshold : achievement.Threshold;
        if (newMetric == AchievementMetric.DistinctCompletedLearners && newTrack != Domain.Enums.AchievementTrack.Companion)
        {
            return Result<AdminAchievementDto>.Failure("INVALID_ACHIEVEMENT_METRIC", "Distinct completed learners metric only supports companion track.");
        }

        if (request.HasIconUrl)
        {
            var iconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl.Trim();
            if (iconUrl is not null && !_objectStorageService.IsPublicUrl(iconUrl))
            {
                return Result<AdminAchievementDto>.Failure("INVALID_ACHIEVEMENT_ICON_URL", "Achievement icon URL is invalid.");
            }

            achievement.IconUrl = iconUrl;
        }

        if (request.HasName)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<AdminAchievementDto>.Failure("INVALID_ACHIEVEMENT_NAME", "Achievement name is invalid.");
            }

            var nameExists = await _context.AchievementDefinitions
                .AnyAsync(item => item.AchievementDefinitionId != request.AchievementId && item.Name == name, cancellationToken);
            if (nameExists)
            {
                return Result<AdminAchievementDto>.Failure("ACHIEVEMENT_NAME_EXISTS", "Achievement name already exists.");
            }

            achievement.Name = name;
        }

        if (request.HasDescription)
        {
            var description = request.Description?.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                return Result<AdminAchievementDto>.Failure("INVALID_ACHIEVEMENT_DESCRIPTION", "Achievement description is invalid.");
            }

            achievement.Description = description;
        }

        var shouldResetEffectiveDate =
            (request.HasTrack && newTrack != achievement.Track)
            || (request.HasMetric && newMetric != achievement.Metric)
            || (request.HasThreshold && newThreshold != achievement.Threshold);

        achievement.Track = newTrack;
        achievement.Metric = newMetric;
        achievement.Threshold = newThreshold;

        if (request.HasSortOrder && request.SortOrder.HasValue)
        {
            achievement.SortOrder = request.SortOrder.Value;
        }

        if (request.HasIsActive && request.IsActive.HasValue)
        {
            achievement.IsActive = request.IsActive.Value;
        }

        if (shouldResetEffectiveDate)
        {
            achievement.EffectiveFromUtc = _dateTimeProvider.UtcNow;
        }

        achievement.UpdatedAt = _dateTimeProvider.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<AdminAchievementDto>.Success(AchievementDtoMapper.MapAdmin(achievement));
    }
}
