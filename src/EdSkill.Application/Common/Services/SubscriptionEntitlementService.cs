using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Common.Services;

public class SubscriptionEntitlementService : ISubscriptionEntitlementService
{
    private static readonly TimeSpan SubscriptionDuration = TimeSpan.FromDays(30);

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPointLedgerService _pointLedgerService;

    public SubscriptionEntitlementService(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IPointLedgerService pointLedgerService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _pointLedgerService = pointLedgerService;
    }

    public async Task<ResolvedSubscriptionEntitlements> GetResolvedEntitlementsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var results = await GetResolvedEntitlementsAsync(new[] { userId }, cancellationToken);
        return results.TryGetValue(userId, out var entitlements)
            ? entitlements
            : ResolvedSubscriptionEntitlements.Empty;
    }

    public async Task<IReadOnlyDictionary<Guid, ResolvedSubscriptionEntitlements>> GetResolvedEntitlementsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, ResolvedSubscriptionEntitlements>();
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var subscriptions = await _context.UserSubscriptions
            .AsNoTracking()
            .Include(subscription => subscription.Plan)
            .Where(subscription =>
                userIds.Contains(subscription.UserId)
                && subscription.Status == UserSubscriptionStatus.Active
                && subscription.ExpiresAt > utcNow
                && subscription.Plan != null
                && subscription.Plan.IsActive)
            .OrderBy(subscription => subscription.ExpiresAt)
            .ToListAsync(cancellationToken);

        return userIds.ToDictionary(
            userId => userId,
            userId => BuildEntitlements(subscriptions.Where(subscription => subscription.UserId == userId).ToList()));
    }

    public async Task<IReadOnlyCollection<ActiveUserSubscription>> GetActiveSubscriptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var subscriptions = await _context.UserSubscriptions
            .AsNoTracking()
            .Include(subscription => subscription.Plan)
            .Where(subscription =>
                subscription.UserId == userId
                && subscription.Status == UserSubscriptionStatus.Active
                && subscription.ExpiresAt > utcNow
                && subscription.Plan != null
                && subscription.Plan.IsActive)
            .OrderBy(subscription => subscription.ExpiresAt)
            .ToListAsync(cancellationToken);

        return subscriptions.Select(MapActiveSubscription).ToList();
    }

    public async Task<Result<SubscriptionActivationResult>> ActivatePaidSubscriptionAsync(
        PaymentTransaction payment,
        SubscriptionPlan plan,
        CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var overlap = await _context.UserSubscriptions
            .Include(subscription => subscription.Plan)
            .Where(subscription =>
                subscription.UserId == payment.UserId
                && subscription.Status == UserSubscriptionStatus.Active
                && subscription.ExpiresAt > utcNow
                && subscription.Plan != null
                && subscription.Plan.IsActive)
            .AnyAsync(subscription =>
                (subscription.Plan!.TargetRole == SubscriptionTargetRole.Learner && (plan.TargetRole == SubscriptionTargetRole.Learner || plan.TargetRole == SubscriptionTargetRole.MultiRole))
                || (subscription.Plan!.TargetRole == SubscriptionTargetRole.Companion && (plan.TargetRole == SubscriptionTargetRole.Companion || plan.TargetRole == SubscriptionTargetRole.MultiRole))
                || (subscription.Plan!.TargetRole == SubscriptionTargetRole.MultiRole && (plan.TargetRole == SubscriptionTargetRole.Learner || plan.TargetRole == SubscriptionTargetRole.Companion || plan.TargetRole == SubscriptionTargetRole.MultiRole)),
                cancellationToken);

        if (overlap)
        {
            return Result<SubscriptionActivationResult>.Failure("SUBSCRIPTION_PLAN_CONFLICT", "Subscription plan conflicts with an active subscription.");
        }

        var startedAt = payment.PaidAt ?? utcNow;
        var expiresAt = startedAt.Add(SubscriptionDuration);
        var userSubscription = new UserSubscription
        {
            UserSubscriptionId = Guid.NewGuid(),
            UserId = payment.UserId,
            PlanId = plan.SubscriptionPlanId,
            PaymentTransactionId = payment.PaymentTransactionId,
            Status = UserSubscriptionStatus.Active,
            StartedAt = startedAt,
            ExpiresAt = expiresAt,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await _context.UserSubscriptions.AddAsync(userSubscription, cancellationToken);

        if (plan.ImmediateBonusPoints > 0)
        {
            var wallet = await _pointLedgerService.GetOrCreateWalletAsync(payment.UserId, cancellationToken);
            var bonusResult = _pointLedgerService.CreditUser(
                wallet,
                PointTransactionType.SubscriptionPurchaseBonus,
                plan.ImmediateBonusPoints,
                null,
                $"Subscription bonus for {plan.Name}.");
            if (!bonusResult.IsSuccess)
            {
                return Result<SubscriptionActivationResult>.Failure(bonusResult.ErrorCode!, bonusResult.ErrorMessage!);
            }
        }

        return Result<SubscriptionActivationResult>.Success(
            new SubscriptionActivationResult(
                userSubscription.UserSubscriptionId,
                plan.SubscriptionPlanId,
                plan.Name,
                startedAt,
                expiresAt,
                plan.ImmediateBonusPoints));
    }

    public async Task<Result<SubscriptionWeeklyBonusResult>> ApplyWeeklyCompletionBonusesAsync(Session session, CancellationToken cancellationToken)
    {
        if (!session.LearnerId.HasValue)
        {
            return Result<SubscriptionWeeklyBonusResult>.Success(new SubscriptionWeeklyBonusResult(0, 0));
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var weekStartUtc = VietnamTimeWindowHelper.GetWeekStartUtc(utcNow);

        var entitlementLookup = await GetResolvedEntitlementsAsync(
            new[] { session.LearnerId.Value, session.CompanionId },
            cancellationToken);

        var learnerBonus = 0;
        var companionBonus = 0;

        if (entitlementLookup.TryGetValue(session.LearnerId.Value, out var learnerEntitlements)
            && learnerEntitlements.WeeklyLearnerSessionBonusPoints > 0)
        {
            var hasCompletedSessionThisWeek = await _context.Sessions
                .AsNoTracking()
                .AnyAsync(item =>
                    item.LearnerId == session.LearnerId.Value
                    && item.Status == SessionStatus.Completed
                    && item.DisbursedAt.HasValue
                    && item.DisbursedAt.Value >= weekStartUtc,
                    cancellationToken);

            if (!hasCompletedSessionThisWeek)
            {
                var learnerWallet = await _pointLedgerService.GetOrCreateWalletAsync(session.LearnerId.Value, cancellationToken);
                var learnerResult = _pointLedgerService.CreditUser(
                    learnerWallet,
                    PointTransactionType.SubscriptionWeeklySessionBonus,
                    learnerEntitlements.WeeklyLearnerSessionBonusPoints,
                    session.SessionId,
                    "Weekly learner subscription bonus.");
                if (!learnerResult.IsSuccess)
                {
                    return Result<SubscriptionWeeklyBonusResult>.Failure(learnerResult.ErrorCode!, learnerResult.ErrorMessage!);
                }

                learnerBonus = learnerEntitlements.WeeklyLearnerSessionBonusPoints;
            }
        }

        if (entitlementLookup.TryGetValue(session.CompanionId, out var companionEntitlements)
            && companionEntitlements.WeeklyCompanionSessionBonusPoints > 0)
        {
            var hasCompletedSessionThisWeek = await _context.Sessions
                .AsNoTracking()
                .AnyAsync(item =>
                    item.CompanionId == session.CompanionId
                    && item.Status == SessionStatus.Completed
                    && item.DisbursedAt.HasValue
                    && item.DisbursedAt.Value >= weekStartUtc,
                    cancellationToken);

            if (!hasCompletedSessionThisWeek)
            {
                var companionWallet = await _pointLedgerService.GetOrCreateWalletAsync(session.CompanionId, cancellationToken);
                var companionResult = _pointLedgerService.CreditUser(
                    companionWallet,
                    PointTransactionType.SubscriptionWeeklySessionBonus,
                    companionEntitlements.WeeklyCompanionSessionBonusPoints,
                    session.SessionId,
                    "Weekly companion subscription bonus.");
                if (!companionResult.IsSuccess)
                {
                    return Result<SubscriptionWeeklyBonusResult>.Failure(companionResult.ErrorCode!, companionResult.ErrorMessage!);
                }

                companionBonus = companionEntitlements.WeeklyCompanionSessionBonusPoints;
            }
        }

        return Result<SubscriptionWeeklyBonusResult>.Success(new SubscriptionWeeklyBonusResult(learnerBonus, companionBonus));
    }

    private static ResolvedSubscriptionEntitlements BuildEntitlements(IReadOnlyCollection<UserSubscription> subscriptions)
    {
        if (subscriptions.Count == 0)
        {
            return ResolvedSubscriptionEntitlements.Empty;
        }

        var activeSubscriptions = subscriptions
            .Where(subscription => subscription.Plan != null)
            .Select(MapActiveSubscription)
            .ToList();

        var learnerPlan = subscriptions.FirstOrDefault(subscription => CoversLearner(subscription.Plan!.TargetRole))?.Plan;
        var companionPlan = subscriptions.FirstOrDefault(subscription => CoversCompanion(subscription.Plan!.TargetRole))?.Plan;

        return new ResolvedSubscriptionEntitlements(
            activeSubscriptions,
            learnerPlan is not null,
            companionPlan is not null,
            companionPlan?.CompanionBadgeText,
            companionPlan?.HasPriorityVisibility ?? false,
            companionPlan?.CompanionDailySessionLimitOverride,
            learnerPlan?.LearnerTokenRewardRatePercent,
            companionPlan?.CompanionTokenRewardRatePercent,
            learnerPlan?.WeeklyLearnerSessionBonusPoints ?? 0,
            companionPlan?.WeeklyCompanionSessionBonusPoints ?? 0);
    }

    private static ActiveUserSubscription MapActiveSubscription(UserSubscription subscription)
    {
        return new ActiveUserSubscription(
            subscription.UserSubscriptionId,
            subscription.PlanId,
            subscription.Plan!.Code,
            subscription.Plan.Name,
            subscription.Plan.TargetRole,
            subscription.Status,
            subscription.StartedAt,
            subscription.ExpiresAt,
            subscription.Plan.CompanionBadgeText,
            subscription.Plan.HasPriorityVisibility);
    }

    public static bool HasCoverageOverlap(SubscriptionTargetRole left, SubscriptionTargetRole right)
    {
        return (CoversLearner(left) && CoversLearner(right))
            || (CoversCompanion(left) && CoversCompanion(right));
    }

    public static bool CoversLearner(SubscriptionTargetRole targetRole)
    {
        return targetRole is SubscriptionTargetRole.Learner or SubscriptionTargetRole.MultiRole;
    }

    public static bool CoversCompanion(SubscriptionTargetRole targetRole)
    {
        return targetRole is SubscriptionTargetRole.Companion or SubscriptionTargetRole.MultiRole;
    }
}
