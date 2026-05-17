using EdSkill.Application.Features.Wallet.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Wallet;

public static class WalletDtoMapper
{
    public static PointWalletSummaryDto MapSummary(PointWallet wallet)
    {
        return new PointWalletSummaryDto(
            wallet.Balance,
            wallet.HeldBalance,
            wallet.TotalEarned,
            wallet.TotalSpent);
    }

    public static PointTransactionDto MapTransaction(PointTransaction transaction)
    {
        return new PointTransactionDto(
            transaction.PointTransactionId,
            transaction.Type,
            transaction.Amount,
            transaction.BalanceBefore,
            transaction.BalanceAfter,
            transaction.HeldBalanceBefore,
            transaction.HeldBalanceAfter,
            transaction.SessionId,
            transaction.Note,
            transaction.CreatedAt);
    }

    public static PointPackageDto MapPackage(PointPackage package)
    {
        return new PointPackageDto(
            package.PointPackageId,
            package.Code,
            package.Name,
            package.Description,
            package.Points,
            package.BonusPoints,
            package.Points + package.BonusPoints,
            package.PriceVnd,
            package.Currency,
            package.BadgeText,
            package.IsHighlighted);
    }

    public static PaymentTransactionDto MapPaymentTransaction(
        PaymentTransaction payment,
        string? packageName,
        string? subscriptionPlanName)
    {
        return new PaymentTransactionDto(
            payment.PaymentTransactionId,
            payment.PointPackageId,
            packageName,
            payment.SubscriptionPlanId,
            subscriptionPlanName,
            payment.Provider,
            payment.AmountVnd,
            payment.Currency,
            payment.Status,
            payment.PaymentUrl,
            payment.PaidAt,
            payment.CreatedAt);
    }
}
