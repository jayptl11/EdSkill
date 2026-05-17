using EdSkill.Domain.Enums;

namespace EdSkill.Application.Common.Models;

public record VnPayCreatePaymentRequest(
    Guid PaymentTransactionId,
    Guid UserId,
    int AmountVnd,
    string OrderDescription,
    DateTime CreatedAtUtc
);

public record VnPayCreatePaymentResult(
    string PaymentUrl,
    DateTime ExpiresAtUtc,
    string TransactionRef
);

public record VnPayCallbackParseResult(
    Guid PaymentTransactionId,
    PaymentStatus Status,
    string? ProviderTransactionId,
    int AmountVnd,
    DateTime? PaidAtUtc,
    IReadOnlyDictionary<string, string> RawData
);

public record WalletPaymentProcessingResult(
    Guid PaymentTransactionId,
    PaymentStatus Status,
    Guid? PointPackageId,
    string? PackageName,
    Guid? SubscriptionPlanId,
    string? SubscriptionPlanName,
    int CreditedPoints,
    bool AlreadyProcessed
);
