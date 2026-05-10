using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Skills.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Skills.Queries.SearchSkills;

public class SearchSkillsQueryHandler : IRequestHandler<SearchSkillsQuery, Result<IReadOnlyCollection<SkillDto>>>
{
    private readonly IApplicationDbContext _context;

    public SearchSkillsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyCollection<SkillDto>>> Handle(SearchSkillsQuery request, CancellationToken cancellationToken)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(request.Query)
            ? null
            : SkillNormalization.NormalizeLookup(request.Query);
        var normalizedCategory = string.IsNullOrWhiteSpace(request.Category)
            ? null
            : SkillNormalization.NormalizeLookup(request.Category);

        var skills = await _context.Skills
            .AsNoTracking()
            .Where(skill => skill.IsActive && !skill.IsDeleted)
            .OrderBy(skill => skill.Name)
            .ToListAsync(cancellationToken);

        var filtered = skills
            .Where(skill => normalizedCategory is null ||
                (!string.IsNullOrWhiteSpace(skill.Category) &&
                 SkillNormalization.NormalizeLookup(skill.Category) == normalizedCategory))
            .Where(skill => normalizedQuery is null || MatchesQuery(skill, normalizedQuery))
            .Take(Math.Clamp(request.Limit, 1, 100))
            .Select(SkillDtoMapper.Map)
            .ToList();

        return Result<IReadOnlyCollection<SkillDto>>.Success(filtered);
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
