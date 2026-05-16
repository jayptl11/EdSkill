using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Commands.UpdateAchievement;

public record UpdateAchievementCommand(
    Guid AchievementId,
    bool HasName,
    string? Name,
    bool HasDescription,
    string? Description,
    bool HasIconUrl,
    string? IconUrl,
    bool HasTrack,
    string? Track,
    bool HasMetric,
    string? Metric,
    bool HasThreshold,
    int? Threshold,
    bool HasSortOrder,
    int? SortOrder,
    bool HasIsActive,
    bool? IsActive) : IRequest<Result<AdminAchievementDto>>;
