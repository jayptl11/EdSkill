using EdSkill.Application.Common.Models;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;

namespace EdSkill.Application.Common.Interfaces;

public interface IPointLedgerService
{
    Task<PointWallet> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken);
    Task<SystemLedgerAccount> GetPlatformLedgerAsync(CancellationToken cancellationToken);
    Task<Result> ApplySignupBonusAsync(Guid userId, int amount, string? note, CancellationToken cancellationToken);
    Result HoldPoints(PointWallet wallet, int amount, Guid sessionId, string? note = null);
    Result ReleaseHeldPoints(PointWallet wallet, int amount, Guid sessionId, PointTransactionType type, string? note = null);
    Result CompleteSessionPayment(PointWallet wallet, int amount, Guid sessionId, string? note = null);
    Result CreditUser(PointWallet wallet, PointTransactionType type, int amount, Guid? sessionId, string? note = null);
    Result CreditPlatform(SystemLedgerAccount ledgerAccount, int amount, Guid? sessionId, string? note = null);
}
