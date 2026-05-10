using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Skills.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Skills.Commands.CreateSkill;

public record CreateSkillCommand(
    string Name,
    string? Slug,
    string? Category,
    IReadOnlyCollection<string>? Aliases
) : IRequest<Result<AdminSkillDto>>;
