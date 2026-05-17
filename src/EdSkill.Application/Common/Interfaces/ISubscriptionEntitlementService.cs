using EdSkill.Application.Common.Models;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Common.Interfaces;

public interface ISubscriptionEntitlementService
{
    Task<ResolvedSubscriptionEntitlements> GetResolvedEntitlementsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, ResolvedSubscriptionEntitlements>> GetResolvedEntitlementsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ActiveUserSubscription>> GetActiveSubscriptionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<SubscriptionActivationResult>> ActivatePaidSubscriptionAsync(
        PaymentTransaction payment,
        SubscriptionPlan plan,
        CancellationToken cancellationToken);
    Task<Result<SubscriptionWeeklyBonusResult>> ApplyWeeklyCompletionBonusesAsync(Session session, CancellationToken cancellationToken);
}
