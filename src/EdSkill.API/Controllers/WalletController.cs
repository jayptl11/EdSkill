using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using EdSkill.Application.Features.Wallet.Queries.GetMyPointTransactions;
using EdSkill.Application.Features.Wallet.Queries.GetMyPointWallet;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet/points")]
public class WalletController : ControllerBase
{
    private readonly ISender _sender;

    public WalletController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PointWalletSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWallet(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyPointWalletQuery(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PointTransactionHistoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactions(
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetMyPointTransactionsQuery(type, page, limit), cancellationToken);
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
            "POINT_WALLET_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
