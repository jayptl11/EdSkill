using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.Commands.GrantPoints;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/points")]
public class AdminPointsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminPointsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("grant")]
    [ProducesResponseType(typeof(GrantPointsResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GrantPoints([FromBody] GrantPointsRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GrantPointsCommand(request.UserIds, request.Amount, request.Note), cancellationToken);
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
            "USER_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
