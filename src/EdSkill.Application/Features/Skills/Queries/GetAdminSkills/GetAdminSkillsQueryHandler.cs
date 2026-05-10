using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Skills.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Skills.Queries.GetAdminSkills;

public class GetAdminSkillsQueryHandler : IRequestHandler<GetAdminSkillsQuery, Result<IReadOnlyCollection<AdminSkillDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminSkillsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyCollection<AdminSkillDto>>> Handle(GetAdminSkillsQuery request, CancellationToken cancellationToken)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(request.Query)
            ? null
            : SkillNormalization.NormalizeLookup(request.Query);

        var skills = await _context.Skills
            .AsNoTracking()
            .Where(skill => !skill.IsDeleted && (request.IncludeInactive || skill.IsActive))
            .OrderBy(skill => skill.Name)
            .ToListAsync(cancellationToken);

        var filtered = skills
            .Where(skill => normalizedQuery is null || MatchesQuery(skill, normalizedQuery))
            .Select(SkillDtoMapper.MapAdmin)
            .ToList();

        return Result<IReadOnlyCollection<AdminSkillDto>>.Success(filtered);
    }

    private static bool MatchesQuery(Domain.Entities.Skill skill, string normalizedQuery)
    {
        if (SkillNormalization.NormalizeLookup(skill.Name).Contains(normalizedQuery, StringComparison.Ordinal) ||
            SkillNormalization.NormalizeLookup(skill.Slug).Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(skill.Category) &&
            SkillNormalization.NormalizeLookup(skill.Category).Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return true;
        }

        return skill.Aliases.Any(alias =>
            SkillNormalization.NormalizeLookup(alias).Contains(normalizedQuery, StringComparison.Ordinal));
    }
}
