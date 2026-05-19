using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.Commands.RetryPointPurchase;
using EdSkill.Application.Features.Wallet.DTOs;
using EdSkill.Application.Features.Wallet.Queries.GetMyPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet/payments")]
public class WalletPaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public WalletPaymentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaymentTransactionHistoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPayments(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetMyPaymentsQuery(status, page, limit), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{paymentTransactionId:guid}/retry")]
    [ProducesResponseType(typeof(CreatePointPurchaseResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetryPayment(Guid paymentTransactionId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RetryPointPurchaseCommand(paymentTransactionId), cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ErrorCode switch
        {
            "PAYMENT_TRANSACTION_NOT_FOUND" or "POINT_PACKAGE_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorCode, result.ErrorMessage }),
            "POINT_PACKAGE_NOT_AVAILABLE" or "PAYMENT_RETRY_INVALID_STATUS" => Conflict(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
