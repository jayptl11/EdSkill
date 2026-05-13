using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Skills.DTOs;
using EdSkill.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Skills.Commands.CreateSkill;

public class CreateSkillCommandHandler : IRequestHandler<CreateSkillCommand, Result<AdminSkillDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateSkillCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AdminSkillDto>> Handle(CreateSkillCommand request, CancellationToken cancellationToken)
    {
        var name = SkillNormalization.NormalizeWhitespace(request.Name);
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SkillNormalization.GenerateSlug(name)
            : SkillNormalization.GenerateSlug(request.Slug);
        var category = string.IsNullOrWhiteSpace(request.Category)
            ? null
            : SkillNormalization.NormalizeWhitespace(request.Category);
        var aliases = SkillNormalization.NormalizeAliasCollection(request.Aliases);

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result<AdminSkillDto>.Failure("INVALID_SKILL_SLUG", "Skill slug is invalid.");
        }

        var existingSkills = await _context.Skills
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .ToListAsync(cancellationToken);

        var conflictCode = SkillCatalogRules.GetConflictCode(null, name, slug, aliases, existingSkills);
        if (conflictCode is not null)
        {
            return Result<AdminSkillDto>.Failure(conflictCode, "Skill conflicts with an existing skill.");
        }

        var skill = new Skill
        {
            SkillId = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Category = category,
            BasePointCost = request.BasePointCost,
            Aliases = aliases,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Skills.AddAsync(skill, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<AdminSkillDto>.Success(SkillDtoMapper.MapAdmin(skill));
    }
}
