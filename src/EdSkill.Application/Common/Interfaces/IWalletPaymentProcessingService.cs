using EdSkill.Application.Common.Models;

namespace EdSkill.Application.Common.Interfaces;

public interface IWalletPaymentProcessingService
{
    Task<Result<WalletPaymentProcessingResult>> ProcessVnPayCallbackAsync(
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken);
}
