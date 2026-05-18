using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Application.Features.Achievements.Queries.GetMyAchievements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize]
[Route("api/achievements")]
public class MyAchievementsController : ControllerBase
{
    private readonly ISender _sender;

    public MyAchievementsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(MyAchievementsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAchievements(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyAchievementsQuery(), cancellationToken);
        return Ok(result.Value);
    }
}
