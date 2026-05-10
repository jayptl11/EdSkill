using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Skills.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Skills.Commands.UpdateSkill;

public record UpdateSkillCommand(
    Guid SkillId,
    bool HasName,
    string? Name,
    bool HasSlug,
    string? Slug,
    bool HasCategory,
    string? Category,
    bool HasAliases,
    IReadOnlyCollection<string>? Aliases,
    bool HasIsActive,
    bool? IsActive
) : IRequest<Result<AdminSkillDto>>;
