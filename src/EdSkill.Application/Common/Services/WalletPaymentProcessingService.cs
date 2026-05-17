using System.Text.Json;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Common.Services;

public class WalletPaymentProcessingService : IWalletPaymentProcessingService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPointLedgerService _pointLedgerService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IVnPayGatewayService _vnPayGatewayService;

    public WalletPaymentProcessingService(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IPointLedgerService pointLedgerService,
        ITransactionExecutor transactionExecutor,
        IVnPayGatewayService vnPayGatewayService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _pointLedgerService = pointLedgerService;
        _transactionExecutor = transactionExecutor;
        _vnPayGatewayService = vnPayGatewayService;
    }

    public async Task<Result<WalletPaymentProcessingResult>> ProcessVnPayCallbackAsync(
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken)
    {
        var parseResult = _vnPayGatewayService.ParseCallback(payload);
        if (!parseResult.IsSuccess)
        {
            return Result<WalletPaymentProcessingResult>.Failure(parseResult.ErrorCode!, parseResult.ErrorMessage!);
        }

        var callback = parseResult.Value!;
        var payment = await _context.PaymentTransactions
            .FirstOrDefaultAsync(item => item.PaymentTransactionId == callback.PaymentTransactionId, cancellationToken);

        if (payment == null || payment.Provider != PaymentProvider.VnPay)
        {
            return Result<WalletPaymentProcessingResult>.Failure("PAYMENT_TRANSACTION_NOT_FOUND", "Payment transaction was not found.");
        }

        if (payment.AmountVnd != callback.AmountVnd)
        {
            return Result<WalletPaymentProcessingResult>.Failure("PAYMENT_CALLBACK_INVALID", "Payment amount does not match transaction amount.");
        }

        var pointPackage = payment.PointPackageId.HasValue
            ? await _context.PointPackages.FirstOrDefaultAsync(
                item => item.PointPackageId == payment.PointPackageId.Value,
                cancellationToken)
            : null;

        var rawPayload = JsonSerializer.Serialize(callback.RawData);
        var creditedPoints = pointPackage is null ? 0 : pointPackage.Points + pointPackage.BonusPoints;

        if (payment.Status != PaymentStatus.Pending)
        {
            return Result<WalletPaymentProcessingResult>.Success(
                new WalletPaymentProcessingResult(
                    payment.PaymentTransactionId,
                    payment.Status,
                    payment.PointPackageId,
                    pointPackage?.Name,
                    payment.Status == PaymentStatus.Success ? creditedPoints : 0,
                    true));
        }

        return await _transactionExecutor.ExecuteAsync<WalletPaymentProcessingResult>(async ct =>
        {
            payment.ProviderTransactionId = callback.ProviderTransactionId ?? payment.ProviderTransactionId;
            payment.RawPayload = rawPayload;
            payment.UpdatedAt = _dateTimeProvider.UtcNow;

            if (callback.Status == PaymentStatus.Success)
            {
                if (pointPackage == null)
                {
                    return Result<WalletPaymentProcessingResult>.Failure("POINT_PACKAGE_NOT_FOUND", "Point package was not found for this payment.");
                }

                payment.Status = PaymentStatus.Success;
                payment.PaidAt = callback.PaidAtUtc ?? _dateTimeProvider.UtcNow;

                var wallet = await _pointLedgerService.GetOrCreateWalletAsync(payment.UserId, ct);
                var note = $"Purchased package {pointPackage.Name}.";
                var creditResult = _pointLedgerService.CreditUser(wallet, PointTransactionType.Purchase, creditedPoints, null, note);
                if (!creditResult.IsSuccess)
                {
                    return Result<WalletPaymentProcessingResult>.Failure(creditResult.ErrorCode!, creditResult.ErrorMessage!);
                }
            }
            else
            {
                payment.Status = callback.Status;
            }

            return Result<WalletPaymentProcessingResult>.Success(
                new WalletPaymentProcessingResult(
                    payment.PaymentTransactionId,
                    payment.Status,
                    payment.PointPackageId,
                    pointPackage?.Name,
                    payment.Status == PaymentStatus.Success ? creditedPoints : 0,
                    false));
        }, cancellationToken);
    }
}
