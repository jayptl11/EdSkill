using EdSkill.Application.Features.Skills.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Skills;

internal static class SkillDtoMapper
{
    public static SkillDto Map(Skill skill)
    {
        return new SkillDto(skill.SkillId, skill.Name, skill.Slug, skill.Category);
    }

    public static AdminSkillDto MapAdmin(Skill skill)
    {
        return new AdminSkillDto(
            skill.SkillId,
            skill.Name,
            skill.Slug,
            skill.Category,
            skill.BasePointCost,
            skill.Aliases.AsReadOnly(),
            skill.IsActive);
    }
}
