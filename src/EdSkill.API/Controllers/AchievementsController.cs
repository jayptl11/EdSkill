using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Application.Features.Admin.Commands.CreateAchievement;
using EdSkill.Application.Features.Admin.Commands.GenerateAchievementIconUploadUrl;
using EdSkill.Application.Features.Admin.Commands.UpdateAchievement;
using EdSkill.Application.Features.Admin.Queries.GetAchievements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/achievements")]
public class AdminAchievementsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminAchievementsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AdminAchievementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAchievements([FromQuery] bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAchievementsQuery(includeInactive), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminAchievementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAchievement([FromBody] CreateAchievementRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateAchievementCommand(
                request.Name,
                request.Description,
                request.IconUrl,
                request.Track,
                request.Metric,
                request.Threshold,
                request.SortOrder),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPatch("{achievementId:guid}")]
    [ProducesResponseType(typeof(AdminAchievementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAchievement(
        Guid achievementId,
        [FromBody] UpdateAchievementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateAchievementCommand(
                achievementId,
                request.HasName,
                request.Name,
                request.HasDescription,
                request.Description,
                request.HasIconUrl,
                request.IconUrl,
                request.HasTrack,
                request.Track,
                request.HasMetric,
                request.Metric,
                request.HasThreshold,
                request.Threshold,
                request.HasSortOrder,
                request.SortOrder,
                request.HasIsActive,
                request.IsActive),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("icon-upload-url")]
    [ProducesResponseType(typeof(AchievementIconUploadUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateIconUploadUrl(
        [FromBody] GenerateAchievementIconUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GenerateAchievementIconUploadUrlCommand(request.FileName, request.ContentType, request.FileSize),
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
            "ACHIEVEMENT_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "ACHIEVEMENT_NAME_EXISTS" => Conflict(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
