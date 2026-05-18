using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.MySpace.Queries.GetMySpace;

public class GetMySpaceQueryHandler : IRequestHandler<GetMySpaceQuery, Result<MySpaceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMySpaceQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<MySpaceDto>> Handle(GetMySpaceQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();

        var companionCards = await _context.CompanionSpaceCards
            .AsNoTracking()
            .Include(card => card.Skill)
            .Where(card => card.UserId == userId)
            .OrderByDescending(card => card.UpdatedAt)
            .ThenBy(card => card.Title)
            .ToListAsync(cancellationToken);

        var learnerCards = await _context.LearnerSpaceCards
            .AsNoTracking()
            .Include(card => card.Skill)
            .Where(card => card.UserId == userId)
            .OrderByDescending(card => card.UpdatedAt)
            .ThenBy(card => card.Title)
            .ToListAsync(cancellationToken);

        return Result<MySpaceDto>.Success(MySpaceDtoMapper.Map(companionCards, learnerCards));
    }
}
