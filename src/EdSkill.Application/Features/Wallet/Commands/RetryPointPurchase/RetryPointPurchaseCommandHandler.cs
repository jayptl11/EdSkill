using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Wallet.Commands.RetryPointPurchase;

public class RetryPointPurchaseCommandHandler : IRequestHandler<RetryPointPurchaseCommand, Result<CreatePointPurchaseResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContextService _requestContextService;
    private readonly IVnPayGatewayService _vnPayGatewayService;

    public RetryPointPurchaseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IRequestContextService requestContextService,
        IVnPayGatewayService vnPayGatewayService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _requestContextService = requestContextService;
        _vnPayGatewayService = vnPayGatewayService;
    }

    public async Task<Result<CreatePointPurchaseResultDto>> Handle(RetryPointPurchaseCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (user == null || !HasPurchasableRole(user.Roles))
        {
            return Result<CreatePointPurchaseResultDto>.Failure("FORBIDDEN", "Current user is not allowed to purchase point packages.");
        }

        var sourcePayment = await _context.PaymentTransactions
            .FirstOrDefaultAsync(item => item.PaymentTransactionId == request.PaymentTransactionId, cancellationToken);
        if (sourcePayment == null)
        {
            return Result<CreatePointPurchaseResultDto>.Failure("PAYMENT_TRANSACTION_NOT_FOUND", "Payment transaction was not found.");
        }

        if (sourcePayment.UserId != userId)
        {
            return Result<CreatePointPurchaseResultDto>.Failure("FORBIDDEN", "You do not have access to this payment transaction.");
        }

        if (!sourcePayment.PointPackageId.HasValue || sourcePayment.SubscriptionPlanId.HasValue)
        {
            return Result<CreatePointPurchaseResultDto>.Failure("PAYMENT_RETRY_NOT_SUPPORTED", "Only point package payments can be retried.");
        }

        if (sourcePayment.Status is PaymentStatus.Success or PaymentStatus.Refunded)
        {
            return Result<CreatePointPurchaseResultDto>.Failure("PAYMENT_RETRY_INVALID_STATUS", "This payment cannot be retried.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var package = await _context.PointPackages
            .FirstOrDefaultAsync(item => item.PointPackageId == sourcePayment.PointPackageId.Value, cancellationToken);

        if (package == null || package.IsDeleted)
        {
            return Result<CreatePointPurchaseResultDto>.Failure("POINT_PACKAGE_NOT_FOUND", "Point package was not found.");
        }

        if (!PointPackageRules.IsAvailableForSale(package, utcNow))
        {
            return Result<CreatePointPurchaseResultDto>.Failure("POINT_PACKAGE_NOT_AVAILABLE", "Point package is not available for purchase.");
        }

        var newPayment = new PaymentTransaction
        {
            PaymentTransactionId = Guid.NewGuid(),
            UserId = userId,
            PointPackageId = package.PointPackageId,
            Provider = PaymentProvider.VnPay,
            AmountVnd = sourcePayment.AmountVnd,
            Currency = sourcePayment.Currency,
            Status = PaymentStatus.Pending,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        var paymentUrlResult = _vnPayGatewayService.CreatePaymentUrl(
            new VnPayCreatePaymentRequest(
                newPayment.PaymentTransactionId,
                userId,
                newPayment.AmountVnd,
                $"Nap diem {package.Name}",
                utcNow,
                VnPayPaymentPurpose.PointPurchase,
                _requestContextService.GetClientIpAddress()));

        if (!paymentUrlResult.IsSuccess)
        {
            return Result<CreatePointPurchaseResultDto>.Failure(paymentUrlResult.ErrorCode!, paymentUrlResult.ErrorMessage!);
        }

        newPayment.PaymentUrl = paymentUrlResult.Value!.PaymentUrl;

        if (sourcePayment.Status == PaymentStatus.Pending)
        {
            sourcePayment.Status = PaymentStatus.Cancelled;
            sourcePayment.UpdatedAt = utcNow;
        }

        await _context.PaymentTransactions.AddAsync(newPayment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreatePointPurchaseResultDto>.Success(
            new CreatePointPurchaseResultDto(
                newPayment.PaymentTransactionId,
                newPayment.PaymentUrl,
                paymentUrlResult.Value.ExpiresAtUtc));
    }

    private static bool HasPurchasableRole(IReadOnlyCollection<string> roles)
    {
        return roles.Any(role =>
            role.Equals("learner", StringComparison.OrdinalIgnoreCase)
            || role.Equals("companion", StringComparison.OrdinalIgnoreCase));
    }
}
