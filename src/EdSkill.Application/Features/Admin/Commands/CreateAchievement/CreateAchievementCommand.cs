using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Commands.CreateAchievement;

public record CreateAchievementCommand(
    string Name,
    string Description,
    string? IconUrl,
    string Track,
    string Metric,
    int Threshold,
    int SortOrder) : IRequest<Result<AdminAchievementDto>>;
