using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.Commands.CreateSubscriptionPurchase;
using EdSkill.Application.Features.Subscriptions.Commands.ProcessSubscriptionVnPayIpnCallback;
using EdSkill.Application.Features.Subscriptions.Commands.ProcessSubscriptionVnPayReturnCallback;
using EdSkill.Application.Features.Subscriptions.DTOs;
using EdSkill.Application.Features.Subscriptions.Queries.GetMySubscriptions;
using EdSkill.Application.Features.Subscriptions.Queries.GetSubscriptionPlans;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet/subscriptions")]
public class WalletSubscriptionsController : ControllerBase
{
    private readonly ISender _sender;

    public WalletSubscriptionsController(ISender sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpGet("plans")]
    [ProducesResponseType(typeof(SubscriptionPlanListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSubscriptionPlansQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(MySubscriptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubscriptions(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMySubscriptionsQuery(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("purchase")]
    [ProducesResponseType(typeof(CreateSubscriptionPurchaseResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreatePurchase([FromBody] CreateSubscriptionPurchaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateSubscriptionPurchaseCommand(request.PlanId), cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("purchase/vnpay-return")]
    [ProducesResponseType(typeof(SubscriptionPurchaseReturnResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessVnPayReturn(CancellationToken cancellationToken)
    {
        var payload = Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
        var result = await _sender.Send(new ProcessSubscriptionVnPayReturnCallbackCommand(payload), cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("purchase/vnpay-ipn")]
    [ProducesResponseType(typeof(VnPayIpnResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessVnPayIpn(CancellationToken cancellationToken)
    {
        var payload = Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
        var result = await _sender.Send(new ProcessSubscriptionVnPayIpnCallbackCommand(payload), cancellationToken);
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
            "SUBSCRIPTION_PLAN_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorCode, result.ErrorMessage }),
            "SUBSCRIPTION_PLAN_CONFLICT" or "SUBSCRIPTION_PLAN_NOT_AVAILABLE" => Conflict(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
