using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Skills.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Skills.Queries.SearchSkills;

public record SearchSkillsQuery(
    string? Query,
    string? Category,
    int Limit = 100
) : IRequest<Result<IReadOnlyCollection<SkillDto>>>;
