using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using EdSkill.Application.Features.Companions.Queries.GetCompanionDetail;
using EdSkill.Application.Features.Companions.Queries.SearchCompanions;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Route("api/companions")]
public class CompanionsController : ControllerBase
{
    private readonly ISender _sender;

    public CompanionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(CompanionSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search(
        [FromQuery] Guid skillId,
        [FromQuery] SessionDeliveryMode? deliveryMode,
        [FromQuery] string? location,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new SearchCompanionsQuery(skillId, deliveryMode, location, page, limit),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{companionId:guid}")]
    [ProducesResponseType(typeof(CompanionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(
        Guid companionId,
        [FromQuery] Guid skillId,
        [FromQuery] SessionDeliveryMode? deliveryMode,
        [FromQuery] string? location,
        [FromQuery] int reviewPage = 1,
        [FromQuery] int reviewLimit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetCompanionDetailQuery(companionId, skillId, deliveryMode, location, reviewPage, reviewLimit),
            cancellationToken);

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
            "SKILL_NOT_FOUND" or "PROFILE_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "PROFILE_PRIVATE" => StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
