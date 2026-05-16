using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Queries.GetAchievements;

public record GetAchievementsQuery(bool IncludeInactive = true) : IRequest<Result<IReadOnlyCollection<AdminAchievementDto>>>;
