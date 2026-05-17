using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.Commands.CreatePointPackage;
using EdSkill.Application.Features.Admin.Commands.DeletePointPackage;
using EdSkill.Application.Features.Admin.Commands.UpdatePointPackage;
using EdSkill.Application.Features.Admin.DTOs;
using EdSkill.Application.Features.Admin.Queries.GetAdminPointPackageById;
using EdSkill.Application.Features.Admin.Queries.GetAdminPointPackages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/point-packages")]
public class AdminPointPackagesController : ControllerBase
{
    private readonly ISender _sender;

    public AdminPointPackagesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AdminPointPackageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackages(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] bool includeInactive = true,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAdminPointPackagesQuery(query, includeInactive, includeDeleted),
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{packageId:guid}")]
    [ProducesResponseType(typeof(AdminPointPackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackageById(Guid packageId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAdminPointPackageByIdQuery(packageId), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminPointPackageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreatePackage([FromBody] CreatePointPackageRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreatePointPackageCommand(
                request.Code,
                request.Name,
                request.Description,
                request.Points,
                request.BonusPoints,
                request.PriceVnd,
                request.BadgeText,
                request.IsHighlighted,
                request.DisplayOrder,
                request.IsActive,
                request.StartsAt,
                request.EndsAt),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPatch("{packageId:guid}")]
    [ProducesResponseType(typeof(AdminPointPackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdatePackage(
        Guid packageId,
        [FromBody] UpdatePointPackageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdatePointPackageCommand(
                packageId,
                request.HasCode,
                request.Code,
                request.HasName,
                request.Name,
                request.HasDescription,
                request.Description,
                request.HasPoints,
                request.Points,
                request.HasBonusPoints,
                request.BonusPoints,
                request.HasPriceVnd,
                request.PriceVnd,
                request.HasBadgeText,
                request.BadgeText,
                request.HasIsHighlighted,
                request.IsHighlighted,
                request.HasDisplayOrder,
                request.DisplayOrder,
                request.HasIsActive,
                request.IsActive,
                request.HasStartsAt,
                request.StartsAt,
                request.HasEndsAt,
                request.EndsAt),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{packageId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePackage(Guid packageId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeletePointPackageCommand(packageId), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok();
        }

        return result.ErrorCode switch
        {
            "POINT_PACKAGE_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ErrorCode switch
        {
            "POINT_PACKAGE_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "POINT_PACKAGE_CODE_EXISTS" => Conflict(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
