using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions;
using EdSkill.Application.Features.Profile.DTOs;
using EdSkill.Application.Features.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Profile.Queries.GetMyProfile;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Result<ProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionEntitlementService _subscriptionEntitlementService;

    public GetMyProfileQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISubscriptionEntitlementService subscriptionEntitlementService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _subscriptionEntitlementService = subscriptionEntitlementService;
    }

    public async Task<Result<ProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetUserId();

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserProfile)
            .Include(u => u.UserSkills)
            .ThenInclude(us => us.Skill)
            .FirstOrDefaultAsync(u => u.UserId == currentUserId, cancellationToken);

        if (user?.UserProfile == null)
        {
            return Result<ProfileDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        var achievements = await CompanionProfileDataLoader.LoadAchievementsAsync(_context, currentUserId, cancellationToken);
        var activeSubscriptions = await _subscriptionEntitlementService.GetActiveSubscriptionsAsync(currentUserId, cancellationToken);
        var entitlements = await _subscriptionEntitlementService.GetResolvedEntitlementsAsync(currentUserId, cancellationToken);

        return Result<ProfileDto>.Success(
            ProfileDtoMapper.Map(
                user,
                user.UserProfile,
                achievements,
                activeSubscriptions: activeSubscriptions.Select(SubscriptionDtoMapper.MapActiveSubscription).ToList(),
                subscriptionEntitlements: SubscriptionDtoMapper.MapEntitlements(entitlements)));
    }
}
