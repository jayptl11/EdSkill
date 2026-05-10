using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.Commands.UpdateSystemConfig;
using EdSkill.Application.Features.Admin.DTOs;
using EdSkill.Application.Features.Admin.Queries.GetSystemConfigs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/config")]
public class AdminConfigController : ControllerBase
{
    private readonly ISender _sender;

    public AdminConfigController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SystemConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfigs(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSystemConfigsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(typeof(SystemConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateConfig(string key, [FromBody] UpdateSystemConfigRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateSystemConfigCommand(key, request.Value), cancellationToken);
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
            "SYSTEM_CONFIG_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
