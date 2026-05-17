using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Subscriptions.Queries.GetMySubscriptions;

public class GetMySubscriptionsQueryHandler : IRequestHandler<GetMySubscriptionsQuery, Result<MySubscriptionsDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionEntitlementService _subscriptionEntitlementService;

    public GetMySubscriptionsQueryHandler(
        ICurrentUserService currentUserService,
        ISubscriptionEntitlementService subscriptionEntitlementService)
    {
        _currentUserService = currentUserService;
        _subscriptionEntitlementService = subscriptionEntitlementService;
    }

    public async Task<Result<MySubscriptionsDto>> Handle(GetMySubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var activeSubscriptions = await _subscriptionEntitlementService.GetActiveSubscriptionsAsync(userId, cancellationToken);
        var entitlements = await _subscriptionEntitlementService.GetResolvedEntitlementsAsync(userId, cancellationToken);

        return Result<MySubscriptionsDto>.Success(
            new MySubscriptionsDto(
                activeSubscriptions.Select(SubscriptionDtoMapper.MapActiveSubscription).ToList(),
                SubscriptionDtoMapper.MapEntitlements(entitlements)));
    }
}
