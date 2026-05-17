using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Wallet.Queries.GetMyPayments;

public class GetMyPaymentsQueryHandler : IRequestHandler<GetMyPaymentsQuery, Result<PaymentTransactionHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyPaymentsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PaymentTransactionHistoryDto>> Handle(GetMyPaymentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var query = _context.PaymentTransactions
            .AsNoTracking()
            .Where(item => item.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<PaymentStatus>(request.Status, true, out var paymentStatus))
            {
                return Result<PaymentTransactionHistoryDto>.Failure("PAYMENT_STATUS_INVALID", "Payment status is invalid.");
            }

            query = query.Where(item => item.Status == paymentStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var packageIds = items
            .Where(item => item.PointPackageId.HasValue)
            .Select(item => item.PointPackageId!.Value)
            .Distinct()
            .ToList();
        var subscriptionPlanIds = items
            .Where(item => item.SubscriptionPlanId.HasValue)
            .Select(item => item.SubscriptionPlanId!.Value)
            .Distinct()
            .ToList();

        var packageLookup = packageIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.PointPackages
                .AsNoTracking()
                .Where(item => packageIds.Contains(item.PointPackageId))
                .ToDictionaryAsync(item => item.PointPackageId, item => item.Name, cancellationToken);
        var subscriptionPlanLookup = subscriptionPlanIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.SubscriptionPlans
                .AsNoTracking()
                .Where(item => subscriptionPlanIds.Contains(item.SubscriptionPlanId))
                .ToDictionaryAsync(item => item.SubscriptionPlanId, item => item.Name, cancellationToken);

        var result = items
            .Select(item => WalletDtoMapper.MapPaymentTransaction(
                item,
                item.PointPackageId.HasValue && packageLookup.TryGetValue(item.PointPackageId.Value, out var packageName)
                    ? packageName
                    : null,
                item.SubscriptionPlanId.HasValue && subscriptionPlanLookup.TryGetValue(item.SubscriptionPlanId.Value, out var subscriptionPlanName)
                    ? subscriptionPlanName
                    : null))
            .ToList();

        return Result<PaymentTransactionHistoryDto>.Success(
            new PaymentTransactionHistoryDto(result, total, request.Page, request.Limit));
    }
}
