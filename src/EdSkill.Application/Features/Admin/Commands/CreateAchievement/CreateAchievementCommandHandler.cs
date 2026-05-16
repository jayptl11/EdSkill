using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements;
using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Commands.CreateAchievement;

public class CreateAchievementCommandHandler : IRequestHandler<CreateAchievementCommand, Result<AdminAchievementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IObjectStorageService _objectStorageService;

    public CreateAchievementCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IObjectStorageService objectStorageService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _objectStorageService = objectStorageService;
    }

    public async Task<Result<AdminAchievementDto>> Handle(CreateAchievementCommand request, CancellationToken cancellationToken)
    {
        if (!AchievementParsing.TryParseTrack(request.Track, out var track)
            || !AchievementParsing.TryParseMetric(request.Metric, out var metric))
        {
            return Result<AdminAchievementDto>.Failure("INVALID_ACHIEVEMENT_RULE", "Achievement rule is invalid.");
        }

        var name = request.Name.Trim();
        var description = request.Description.Trim();
        var iconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl.Trim();

        if (iconUrl is not null && !_objectStorageService.IsPublicUrl(iconUrl))
        {
            return Result<AdminAchievementDto>.Failure("INVALID_ACHIEVEMENT_ICON_URL", "Achievement icon URL is invalid.");
        }

        var exists = await _context.AchievementDefinitions
            .AnyAsync(item => item.Name == name, cancellationToken);
        if (exists)
        {
            return Result<AdminAchievementDto>.Failure("ACHIEVEMENT_NAME_EXISTS", "Achievement name already exists.");
        }

        var now = _dateTimeProvider.UtcNow;
        var achievement = new AchievementDefinition
        {
            AchievementDefinitionId = Guid.NewGuid(),
            Name = name,
            Description = description,
            IconUrl = iconUrl,
            Track = track,
            Metric = metric,
            Threshold = request.Threshold,
            SortOrder = request.SortOrder,
            IsActive = true,
            EffectiveFromUtc = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.AchievementDefinitions.AddAsync(achievement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<AdminAchievementDto>.Success(AchievementDtoMapper.MapAdmin(achievement));
    }
}
