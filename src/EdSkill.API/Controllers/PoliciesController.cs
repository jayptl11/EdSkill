using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Policies.Commands.AcceptMyPolicies;
using EdSkill.Application.Features.Policies.DTOs;
using EdSkill.Application.Features.Policies.Queries.GetActivePolicies;
using EdSkill.Application.Features.Policies.Queries.GetMyPolicyConsents;
using EdSkill.Application.Features.Policies.Queries.GetPolicyBySlug;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Route("api/policies")]
public class PoliciesController : ControllerBase
{
    private readonly ISender _sender;

    public PoliciesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PolicyDocumentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPolicies(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetActivePoliciesQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PolicyDocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPolicyBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetPolicyBySlugQuery(slug), cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpGet("consents/me")]
    [ProducesResponseType(typeof(PolicyConsentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyPolicyConsents(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetMyPolicyConsentsQuery(), cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("consents/me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcceptMyPolicies([FromBody] AcceptPoliciesRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new AcceptMyPoliciesCommand(request.AcceptedPolicies), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok();
        }

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
            "POLICY_DOCUMENT_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return result.ErrorCode switch
        {
            "POLICY_DOCUMENT_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
