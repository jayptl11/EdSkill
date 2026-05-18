using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Reviews.Commands.CreateReview;
using EdSkill.Application.Features.Reviews.DTOs;
using EdSkill.Application.Features.Reviews.Queries.GetMyReviewDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public ReviewsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me/dashboard")]
    [ProducesResponseType(typeof(ReviewDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDashboard(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyReviewDashboardQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateReviewCommand(request.SessionId, request.Rating, request.Comment),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        return result.ErrorCode switch
        {
            "SESSION_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "NOT_SESSION_PARTICIPANT" => StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorCode, result.ErrorMessage }),
            "REVIEW_WINDOW_CLOSED" => StatusCode(StatusCodes.Status410Gone, new { result.ErrorCode, result.ErrorMessage }),
            "REVIEW_ALREADY_EXISTS" or "SESSION_INVALID_STATUS" => Conflict(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
