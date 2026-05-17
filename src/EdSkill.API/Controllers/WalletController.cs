using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.Commands.CreatePointPurchase;
using EdSkill.Application.Features.Wallet.Commands.ProcessVnPayIpnCallback;
using EdSkill.Application.Features.Wallet.Commands.ProcessVnPayReturnCallback;
using EdSkill.Application.Features.Wallet.DTOs;
using EdSkill.Application.Features.Wallet.Queries.GetMyPayments;
using EdSkill.Application.Features.Wallet.Queries.GetPointPackages;
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

    [AllowAnonymous]
    [HttpGet("packages")]
    [ProducesResponseType(typeof(PointPackageListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPointPackages(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPointPackagesQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("purchase")]
    [ProducesResponseType(typeof(CreatePointPurchaseResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePointPurchaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreatePointPurchaseCommand(request.PackageId), cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("purchase/vnpay-return")]
    [ProducesResponseType(typeof(VnPayReturnResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessVnPayReturn(CancellationToken cancellationToken)
    {
        var payload = Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
        var result = await _sender.Send(new ProcessVnPayReturnCallbackCommand(payload), cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("purchase/vnpay-ipn")]
    [ProducesResponseType(typeof(VnPayIpnResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessVnPayIpn(CancellationToken cancellationToken)
    {
        var payload = Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
        var result = await _sender.Send(new ProcessVnPayIpnCallbackCommand(payload), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { result.ErrorCode, result.ErrorMessage });
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
            "POINT_PACKAGE_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorCode, result.ErrorMessage }),
            "POINT_PACKAGE_NOT_AVAILABLE" => Conflict(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
