using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Achievements.Queries.GetMyAchievements;

public record GetMyAchievementsQuery : IRequest<Result<MyAchievementsDto>>;
