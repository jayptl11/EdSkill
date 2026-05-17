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

public record PointPackageDto(
    Guid PackageId,
    string Code,
    string Name,
    string? Description,
    int Points,
    int BonusPoints,
    int TotalPoints,
    int PriceVnd,
    string Currency,
    string? BadgeText,
    bool IsHighlighted
);

public record PointPackageListDto(IReadOnlyCollection<PointPackageDto> Data);

public record CreatePointPurchaseRequest(Guid PackageId);

public record CreatePointPurchaseResultDto(
    Guid PaymentTransactionId,
    string PaymentUrl,
    DateTime ExpiresAt
);

public record PaymentTransactionDto(
    Guid PaymentTransactionId,
    Guid? PackageId,
    string? PackageName,
    PaymentProvider Provider,
    int AmountVnd,
    string Currency,
    PaymentStatus Status,
    string? PaymentUrl,
    DateTime? PaidAt,
    DateTime CreatedAt
);

public record PaymentTransactionHistoryDto(
    IReadOnlyCollection<PaymentTransactionDto> Data,
    int Total,
    int Page,
    int Limit
);

public record VnPayReturnResultDto(
    Guid PaymentTransactionId,
    Guid? PackageId,
    string? PackageName,
    PaymentStatus Status,
    int CreditedPoints,
    bool AlreadyProcessed
);

public record VnPayIpnResponseDto(string RspCode, string Message);
