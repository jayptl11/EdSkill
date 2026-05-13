using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Skills.Commands.CreateSkill;
using EdSkill.Application.Features.Skills.Commands.DeleteSkill;
using EdSkill.Application.Features.Skills.Commands.UpdateSkill;
using EdSkill.Application.Features.Skills.DTOs;
using EdSkill.Application.Features.Skills.Queries.GetAdminSkills;
using EdSkill.Application.Features.Skills.Queries.SearchSkills;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Route("api/skills")]
public class SkillsController : ControllerBase
{
    private readonly ISender _sender;

    public SkillsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSkills(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] string? category,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchSkillsQuery(query, category, limit), cancellationToken);
        return Ok(result.Value);
    }
}

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/skills")]
public class AdminSkillsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminSkillsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AdminSkillDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSkills(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAdminSkillsQuery(query, includeInactive), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminSkillDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateSkill([FromBody] CreateSkillRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSkillCommand(request.Name, request.Slug, request.Category, request.BasePointCost, request.Aliases);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetSkills), new { }, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPatch("{skillId:guid}")]
    [ProducesResponseType(typeof(AdminSkillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSkill(Guid skillId, [FromBody] UpdateSkillRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSkillCommand(
            skillId,
            request.HasName,
            request.Name,
            request.HasSlug,
            request.Slug,
            request.HasCategory,
            request.Category,
            request.HasBasePointCost,
            request.BasePointCost,
            request.HasAliases,
            request.Aliases,
            request.HasIsActive,
            request.IsActive);

        var result = await _sender.Send(command, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{skillId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSkill(Guid skillId, CancellationToken cancellationToken)
    {
        var command = new DeleteSkillCommand(skillId);
        var result = await _sender.Send(command, cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok();
        }

        return result.ErrorCode switch
        {
            "SKILL_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
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
            "SKILL_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "SKILL_NAME_EXISTS" or "SKILL_SLUG_EXISTS" or "SKILL_ALIAS_CONFLICT" => Conflict(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
