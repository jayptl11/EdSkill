using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements;
using EdSkill.Application.Features.Achievements.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Queries.GetAchievements;

public class GetAchievementsQueryHandler : IRequestHandler<GetAchievementsQuery, Result<IReadOnlyCollection<AdminAchievementDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAchievementsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyCollection<AdminAchievementDto>>> Handle(GetAchievementsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AchievementDefinitions.AsNoTracking().AsQueryable();
        if (!request.IncludeInactive)
        {
            query = query.Where(item => item.IsActive);
        }

        var achievements = await query
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<AdminAchievementDto>>.Success(
            achievements.Select(AchievementDtoMapper.MapAdmin).ToList());
    }
}
