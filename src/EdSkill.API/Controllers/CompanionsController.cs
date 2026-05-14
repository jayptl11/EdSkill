using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using EdSkill.Application.Features.Companions.Queries.GetCompanionDetail;
using EdSkill.Application.Features.Companions.Queries.SearchCompanions;
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
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchCompanionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new SearchCompanionsQuery(
                request.SkillId,
                request.MinimumDurationMinutes,
                request.MaxLearnerChargePoints,
                request.CredentialCountGroup,
                request.DeliveryMode,
                request.Location,
                request.Page,
                request.Limit),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{companionId:guid}")]
    [ProducesResponseType(typeof(CompanionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetDetail(
        Guid companionId,
        [FromQuery] GetCompanionDetailRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetCompanionDetailQuery(
                companionId,
                request.SkillId,
                request.MinimumDurationMinutes,
                request.MaxLearnerChargePoints,
                request.CredentialCountGroup,
                request.DeliveryMode,
                request.Location,
                request.ReviewPage,
                request.ReviewLimit),
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
