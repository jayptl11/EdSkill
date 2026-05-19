using EdSkill.Application.Common.Models;
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

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ErrorCode switch
        {
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
