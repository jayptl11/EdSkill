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
}
