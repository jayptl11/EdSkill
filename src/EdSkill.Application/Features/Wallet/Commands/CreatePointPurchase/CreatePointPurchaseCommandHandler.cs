using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Wallet.Commands.CreatePointPurchase;

public class CreatePointPurchaseCommandHandler : IRequestHandler<CreatePointPurchaseCommand, Result<CreatePointPurchaseResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContextService _requestContextService;
    private readonly IVnPayGatewayService _vnPayGatewayService;

    public CreatePointPurchaseCommandHandler(
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

    public async Task<Result<CreatePointPurchaseResultDto>> Handle(CreatePointPurchaseCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (user == null || !user.Roles.Any(role =>
                role.Equals("learner", StringComparison.OrdinalIgnoreCase)
                || role.Equals("companion", StringComparison.OrdinalIgnoreCase)))
        {
            return Result<CreatePointPurchaseResultDto>.Failure("FORBIDDEN", "Current user is not allowed to purchase point packages.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var package = await _context.PointPackages
            .FirstOrDefaultAsync(item => item.PointPackageId == request.PackageId, cancellationToken);

        if (package == null || package.IsDeleted)
        {
            return Result<CreatePointPurchaseResultDto>.Failure("POINT_PACKAGE_NOT_FOUND", "Point package was not found.");
        }

        if (!PointPackageRules.IsAvailableForSale(package, utcNow))
        {
            return Result<CreatePointPurchaseResultDto>.Failure("POINT_PACKAGE_NOT_AVAILABLE", "Point package is not available for purchase.");
        }

        var payment = new PaymentTransaction
        {
            PaymentTransactionId = Guid.NewGuid(),
            UserId = userId,
            PointPackageId = package.PointPackageId,
            Provider = PaymentProvider.VnPay,
            AmountVnd = package.PriceVnd,
            Currency = package.Currency,
            Status = PaymentStatus.Pending,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        var paymentUrlResult = _vnPayGatewayService.CreatePaymentUrl(
            new VnPayCreatePaymentRequest(
                payment.PaymentTransactionId,
                userId,
                payment.AmountVnd,
                $"Nap diem {package.Name}",
                utcNow,
                VnPayPaymentPurpose.PointPurchase,
                _requestContextService.GetClientIpAddress()));

        if (!paymentUrlResult.IsSuccess)
        {
            return Result<CreatePointPurchaseResultDto>.Failure(paymentUrlResult.ErrorCode!, paymentUrlResult.ErrorMessage!);
        }

        payment.PaymentUrl = paymentUrlResult.Value!.PaymentUrl;

        await _context.PaymentTransactions.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreatePointPurchaseResultDto>.Success(
            new CreatePointPurchaseResultDto(
                payment.PaymentTransactionId,
                payment.PaymentUrl,
                paymentUrlResult.Value.ExpiresAtUtc));
    }
}
