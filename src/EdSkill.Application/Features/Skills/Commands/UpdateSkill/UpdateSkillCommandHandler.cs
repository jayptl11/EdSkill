using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Skills.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Skills.Commands.UpdateSkill;

public class UpdateSkillCommandHandler : IRequestHandler<UpdateSkillCommand, Result<AdminSkillDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateSkillCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AdminSkillDto>> Handle(UpdateSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = await _context.Skills
            .FirstOrDefaultAsync(x => x.SkillId == request.SkillId, cancellationToken);

        if (skill is null)
        {
            return Result<AdminSkillDto>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
        }

        var name = request.HasName
            ? SkillNormalization.NormalizeWhitespace(request.Name!)
            : skill.Name;
        var slug = request.HasSlug
            ? SkillNormalization.GenerateSlug(request.Slug!)
            : skill.Slug;
        var category = request.HasCategory
            ? string.IsNullOrWhiteSpace(request.Category) ? null : SkillNormalization.NormalizeWhitespace(request.Category)
            : skill.Category;
        var aliases = request.HasAliases
            ? SkillNormalization.NormalizeAliasCollection(request.Aliases)
            : skill.Aliases;

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result<AdminSkillDto>.Failure("INVALID_SKILL_SLUG", "Skill slug is invalid.");
        }

        var existingSkills = await _context.Skills
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var conflictCode = SkillCatalogRules.GetConflictCode(skill.SkillId, name, slug, aliases, existingSkills);
        if (conflictCode is not null)
        {
            return Result<AdminSkillDto>.Failure(conflictCode, "Skill conflicts with an existing skill.");
        }

        skill.Name = name;
        skill.Slug = slug;
        skill.Category = category;
        skill.Aliases = aliases;

        if (request.HasIsActive)
        {
            skill.IsActive = request.IsActive ?? skill.IsActive;
        }

        skill.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<AdminSkillDto>.Success(SkillDtoMapper.MapAdmin(skill));
    }
}
