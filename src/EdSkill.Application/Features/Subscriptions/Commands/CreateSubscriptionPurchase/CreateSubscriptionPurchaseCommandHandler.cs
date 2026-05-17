using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.Services;
using EdSkill.Application.Features.Subscriptions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Subscriptions.Commands.CreateSubscriptionPurchase;

public class CreateSubscriptionPurchaseCommandHandler : IRequestHandler<CreateSubscriptionPurchaseCommand, Result<CreateSubscriptionPurchaseResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IVnPayGatewayService _vnPayGatewayService;

    public CreateSubscriptionPurchaseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IVnPayGatewayService vnPayGatewayService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _vnPayGatewayService = vnPayGatewayService;
    }

    public async Task<Result<CreateSubscriptionPurchaseResultDto>> Handle(CreateSubscriptionPurchaseCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (user == null || !HasPurchasableRole(user.Roles))
        {
            return Result<CreateSubscriptionPurchaseResultDto>.Failure("FORBIDDEN", "Current user is not allowed to purchase subscriptions.");
        }

        var plan = await _context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SubscriptionPlanId == request.PlanId, cancellationToken);
        if (plan == null)
        {
            return Result<CreateSubscriptionPurchaseResultDto>.Failure("SUBSCRIPTION_PLAN_NOT_FOUND", "Subscription plan was not found.");
        }

        if (!plan.IsActive)
        {
            return Result<CreateSubscriptionPurchaseResultDto>.Failure("SUBSCRIPTION_PLAN_NOT_AVAILABLE", "Subscription plan is not available for purchase.");
        }

        if (!CanPurchasePlan(user.Roles, plan.TargetRole))
        {
            return Result<CreateSubscriptionPurchaseResultDto>.Failure("FORBIDDEN", "Current user does not have the required role for this subscription.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var targetRole = plan.TargetRole;
        var hasConflict = await _context.UserSubscriptions
            .AsNoTracking()
            .Include(item => item.Plan)
            .AnyAsync(
                item => item.UserId == userId
                    && item.Status == UserSubscriptionStatus.Active
                    && item.ExpiresAt > utcNow
                    && item.Plan != null
                    && item.Plan.IsActive
                    && ((item.Plan.TargetRole == SubscriptionTargetRole.Learner && (targetRole == SubscriptionTargetRole.Learner || targetRole == SubscriptionTargetRole.MultiRole))
                        || (item.Plan.TargetRole == SubscriptionTargetRole.Companion && (targetRole == SubscriptionTargetRole.Companion || targetRole == SubscriptionTargetRole.MultiRole))
                        || (item.Plan.TargetRole == SubscriptionTargetRole.MultiRole && (targetRole == SubscriptionTargetRole.Learner || targetRole == SubscriptionTargetRole.Companion || targetRole == SubscriptionTargetRole.MultiRole))),
                cancellationToken);
        if (hasConflict)
        {
            return Result<CreateSubscriptionPurchaseResultDto>.Failure("SUBSCRIPTION_PLAN_CONFLICT", "Subscription plan conflicts with an active subscription.");
        }

        var payment = new PaymentTransaction
        {
            PaymentTransactionId = Guid.NewGuid(),
            UserId = userId,
            SubscriptionPlanId = plan.SubscriptionPlanId,
            Provider = PaymentProvider.VnPay,
            AmountVnd = plan.PriceVnd,
            Currency = plan.Currency,
            Status = PaymentStatus.Pending,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        var paymentUrlResult = _vnPayGatewayService.CreatePaymentUrl(
            new VnPayCreatePaymentRequest(
                payment.PaymentTransactionId,
                userId,
                payment.AmountVnd,
                $"Dang ky {plan.Name}",
                utcNow));
        if (!paymentUrlResult.IsSuccess)
        {
            return Result<CreateSubscriptionPurchaseResultDto>.Failure(paymentUrlResult.ErrorCode!, paymentUrlResult.ErrorMessage!);
        }

        payment.PaymentUrl = paymentUrlResult.Value!.PaymentUrl;

        await _context.PaymentTransactions.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateSubscriptionPurchaseResultDto>.Success(
            new CreateSubscriptionPurchaseResultDto(
                payment.PaymentTransactionId,
                payment.PaymentUrl,
                paymentUrlResult.Value.ExpiresAtUtc));
    }

    private static bool HasPurchasableRole(IReadOnlyCollection<string> roles)
    {
        return roles.Any(role =>
            role.Equals("learner", StringComparison.OrdinalIgnoreCase)
            || role.Equals("companion", StringComparison.OrdinalIgnoreCase));
    }

    private static bool CanPurchasePlan(IReadOnlyCollection<string> roles, SubscriptionTargetRole targetRole)
    {
        return targetRole switch
        {
            SubscriptionTargetRole.Learner => roles.Any(role => role.Equals("learner", StringComparison.OrdinalIgnoreCase)),
            SubscriptionTargetRole.Companion => roles.Any(role => role.Equals("companion", StringComparison.OrdinalIgnoreCase)),
            SubscriptionTargetRole.MultiRole => HasPurchasableRole(roles),
            _ => false
        };
    }
}
