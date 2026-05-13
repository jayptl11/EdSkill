using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.Commands.EnableCompanion;
using EdSkill.Application.Features.Profile.Commands.GenerateAvatarUploadUrl;
using EdSkill.Application.Features.Profile.Commands.GenerateDegreeUploadUrl;
using EdSkill.Application.Features.Profile.Commands.UpdateMyProfile;
using EdSkill.Application.Features.Profile.DTOs;
using EdSkill.Application.Features.Profile.Queries.GetMyProfile;
using EdSkill.Application.Features.Profile.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly ISender _sender;

    public ProfileController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyProfileQuery(), cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateMyProfileCommand(
            request.HasDisplayName,
            request.DisplayName,
            request.HasBio,
            request.Bio,
            request.HasDateOfBirth,
            request.DateOfBirth,
            request.HasPhone,
            request.Phone,
            request.HasDegreeUrl,
            request.DegreeUrl,
            request.HasCredentialUrls,
            request.CredentialUrls,
            request.HasSkillsToTeach,
            request.SkillsToTeach,
            request.HasSkillsToLearn,
            request.SkillsToLearn,
            request.HasAvatarUrl,
            request.AvatarUrl,
            request.HasIsPublic,
            request.IsPublic);

        var result = await _sender.Send(command, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("me/degree-upload-url")]
    [ProducesResponseType(typeof(DegreeUploadUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateDegreeUploadUrl([FromBody] GenerateDegreeUploadUrlRequest request, CancellationToken cancellationToken)
    {
        var command = new GenerateDegreeUploadUrlCommand(request.FileName, request.ContentType, request.FileSize);
        var result = await _sender.Send(command, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("me/credential-upload-url")]
    [ProducesResponseType(typeof(DegreeUploadUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateCredentialUploadUrl([FromBody] GenerateDegreeUploadUrlRequest request, CancellationToken cancellationToken)
    {
        var command = new GenerateDegreeUploadUrlCommand(request.FileName, request.ContentType, request.FileSize);
        var result = await _sender.Send(command, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("me/avatar-upload-url")]
    [ProducesResponseType(typeof(AvatarUploadUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateAvatarUploadUrl([FromBody] GenerateAvatarUploadUrlRequest request, CancellationToken cancellationToken)
    {
        var command = new GenerateAvatarUploadUrlCommand(request.FileName, request.ContentType, request.FileSize);
        var result = await _sender.Send(command, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("me/enable-companion")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnableCompanion(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new EnableCompanionCommand(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserProfileQuery(userId), cancellationToken);
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
            "PROFILE_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "PROFILE_PRIVATE" => StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorCode, result.ErrorMessage }),
            "SKILL_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "SKILL_INACTIVE" or "DUPLICATE_SKILL_SELECTION" => BadRequest(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
