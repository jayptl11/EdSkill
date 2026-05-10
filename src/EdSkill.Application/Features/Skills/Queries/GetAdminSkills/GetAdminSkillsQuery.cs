using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Skills.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Skills.Queries.GetAdminSkills;

public record GetAdminSkillsQuery(
    string? Query,
    bool IncludeInactive
) : IRequest<Result<IReadOnlyCollection<AdminSkillDto>>>;
