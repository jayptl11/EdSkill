using EdSkill.Application.Common.Models;

namespace EdSkill.Application.Common.Interfaces;

public interface IVnPayGatewayService
{
    Result<VnPayCreatePaymentResult> CreatePaymentUrl(VnPayCreatePaymentRequest request);
    Result<VnPayCallbackParseResult> ParseCallback(IReadOnlyDictionary<string, string> payload);
}
