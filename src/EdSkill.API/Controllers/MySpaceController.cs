using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.Commands.CreateCompanionSpaceCard;
using EdSkill.Application.Features.MySpace.Commands.CreateLearnerSpaceCard;
using EdSkill.Application.Features.MySpace.Commands.DeleteCompanionSpaceCard;
using EdSkill.Application.Features.MySpace.Commands.DeleteLearnerSpaceCard;
using EdSkill.Application.Features.MySpace.Commands.GenerateMySpaceUploadUrl;
using EdSkill.Application.Features.MySpace.Commands.UpdateCompanionSpaceCard;
using EdSkill.Application.Features.MySpace.Commands.UpdateLearnerSpaceCard;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Application.Features.MySpace.Queries.GetMySpace;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize]
[Route("api/my-space")]
public class MySpaceController : ControllerBase
{
    private readonly ISender _sender;

    public MySpaceController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MySpaceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySpace(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMySpaceQuery(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("companion-cards")]
    [ProducesResponseType(typeof(CompanionSpaceCardDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCompanionCard([FromBody] CreateCompanionSpaceCardRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateCompanionSpaceCardCommand(
                request.SkillId,
                request.Title,
                request.Description,
                request.PricePoints,
                request.DurationMinutes,
                request.DeliveryModes,
                request.Languages,
                request.CoverImageUrl,
                request.CredentialUrls,
                request.IsPublished),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPatch("companion-cards/{cardId:guid}")]
    [ProducesResponseType(typeof(CompanionSpaceCardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCompanionCard(Guid cardId, [FromBody] UpdateCompanionSpaceCardRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateCompanionSpaceCardCommand(
                cardId,
                request.HasSkillId,
                request.SkillId,
                request.HasTitle,
                request.Title,
                request.HasDescription,
                request.Description,
                request.HasPricePoints,
                request.PricePoints,
                request.HasDurationMinutes,
                request.DurationMinutes,
                request.HasDeliveryModes,
                request.DeliveryModes,
                request.HasLanguages,
                request.Languages,
                request.HasCoverImageUrl,
                request.CoverImageUrl,
                request.HasCredentialUrls,
                request.CredentialUrls,
                request.HasIsPublished,
                request.IsPublished),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("companion-cards/{cardId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCompanionCard(Guid cardId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCompanionSpaceCardCommand(cardId), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok();
        }

        return ToActionResult(result);
    }

    [HttpPost("learner-cards")]
    [ProducesResponseType(typeof(LearnerSpaceCardDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLearnerCard([FromBody] CreateLearnerSpaceCardRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateLearnerSpaceCardCommand(
                request.SkillId,
                request.Title,
                request.Description,
                request.TargetPoints,
                request.DurationMinutes,
                request.DeliveryModes,
                request.Languages,
                request.CoverImageUrl,
                request.IsPublished),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPatch("learner-cards/{cardId:guid}")]
    [ProducesResponseType(typeof(LearnerSpaceCardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLearnerCard(Guid cardId, [FromBody] UpdateLearnerSpaceCardRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateLearnerSpaceCardCommand(
                cardId,
                request.HasSkillId,
                request.SkillId,
                request.HasTitle,
                request.Title,
                request.HasDescription,
                request.Description,
                request.HasTargetPoints,
                request.TargetPoints,
                request.HasDurationMinutes,
                request.DurationMinutes,
                request.HasDeliveryModes,
                request.DeliveryModes,
                request.HasLanguages,
                request.Languages,
                request.HasCoverImageUrl,
                request.CoverImageUrl,
                request.HasIsPublished,
                request.IsPublished),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("learner-cards/{cardId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteLearnerCard(Guid cardId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteLearnerSpaceCardCommand(cardId), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok();
        }

        return ToActionResult(result);
    }

    [HttpPost("cover-upload-url")]
    [ProducesResponseType(typeof(MySpaceUploadUrlDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateCoverUploadUrl([FromBody] GenerateMySpaceUploadUrlRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GenerateMySpaceUploadUrlCommand(MySpaceUploadKind.Cover, request.FileName, request.ContentType, request.FileSize),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("credential-upload-url")]
    [ProducesResponseType(typeof(MySpaceUploadUrlDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateCredentialUploadUrl([FromBody] GenerateMySpaceUploadUrlRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GenerateMySpaceUploadUrlCommand(MySpaceUploadKind.Credential, request.FileName, request.ContentType, request.FileSize),
            cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return ToActionResult<object>(Result<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ErrorCode switch
        {
            "PROFILE_NOT_FOUND" or "SKILL_NOT_FOUND" or "MY_SPACE_CARD_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorCode, result.ErrorMessage }),
            "MY_SPACE_SKILL_NOT_OWNED" or "SKILL_INACTIVE" => BadRequest(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
