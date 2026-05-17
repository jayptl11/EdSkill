using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Subscriptions.Queries.GetSubscriptionPlans;

public class GetSubscriptionPlansQueryHandler : IRequestHandler<GetSubscriptionPlansQuery, Result<SubscriptionPlanListDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSubscriptionPlansQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SubscriptionPlanListDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.PriceVnd)
            .ToListAsync(cancellationToken);

        return Result<SubscriptionPlanListDto>.Success(
            new SubscriptionPlanListDto(plans.Select(SubscriptionDtoMapper.MapPlan).ToList()));
    }
}
