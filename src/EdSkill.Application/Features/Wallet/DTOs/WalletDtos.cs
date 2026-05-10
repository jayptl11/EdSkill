using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.Wallet.DTOs;

public record PointWalletSummaryDto(
    int Balance,
    int HeldBalance,
    int TotalEarned,
    int TotalSpent
);

public record PointTransactionDto(
    Guid PointTransactionId,
    PointTransactionType Type,
    int Amount,
    int BalanceBefore,
    int BalanceAfter,
    int HeldBalanceBefore,
    int HeldBalanceAfter,
    Guid? SessionId,
    string? Note,
    DateTime CreatedAt
);

public record PointTransactionHistoryDto(
    IReadOnlyCollection<PointTransactionDto> Data,
    int Total,
    int Page,
    int Limit
);
