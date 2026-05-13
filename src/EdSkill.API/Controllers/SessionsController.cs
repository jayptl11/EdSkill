using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.Commands.BookSession;
using EdSkill.Application.Features.Sessions.Commands.CancelSession;
using EdSkill.Application.Features.Sessions.Commands.ConfirmSession;
using EdSkill.Application.Features.Sessions.Commands.ConfirmSessionCompletion;
using EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;
using EdSkill.Application.Features.Sessions.Commands.JoinSession;
using EdSkill.Application.Features.Sessions.Commands.LeaveSession;
using EdSkill.Application.Features.Sessions.Commands.RejectSession;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Application.Features.Sessions.Queries.GetSessionById;
using EdSkill.Application.Features.Sessions.Queries.GetSessions;
using EdSkill.Application.Features.Sessions.Queries.GetSessionStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdSkill.API.Controllers;

[ApiController]
[Authorize]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISender _sender;

    public SessionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSessionOfferCommand(
            request.SkillId,
            request.Description,
            request.DeliveryMode,
            request.Location,
            request.DurationOptions,
            request.ScheduledAt);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetSessionById), new { id = result.Value!.SessionId }, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(SessionListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(
        [FromQuery] string? status,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetSessionsQuery(status, role, page, limit), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSessionByIdQuery(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/book")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BookSession(Guid id, [FromBody] BookSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new BookSessionCommand(id, request.SelectedDurationMinutes), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ConfirmSessionCommand(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectSession(Guid id, [FromBody] RejectSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RejectSessionCommand(id, request.Reason), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelSession(Guid id, [FromBody] CancelSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelSessionCommand(id, request.Reason), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> JoinSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new JoinSessionCommand(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/leave")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LeaveSession(Guid id, [FromBody] LeaveSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LeaveSessionCommand(id, request.ActualDuration), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/confirm-completion")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmCompletion(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ConfirmSessionCompletionCommand(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(SessionStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSessionStatusQuery(id), cancellationToken);
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
            "SESSION_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "PROFILE_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { result.ErrorCode, result.ErrorMessage }),
            "COMPANION_PROFILE_INCOMPLETE" => UnprocessableEntity(new { result.ErrorCode, result.ErrorMessage }),
            "SESSION_NOT_AVAILABLE" or "SESSION_LIMIT_REACHED" or "SELF_BOOKING" or "INSUFFICIENT_POINTS" or "SESSION_DURATION_INVALID" or "SESSION_NOT_ONLINE" or "INVALID_DURATION_OPTIONS" or "INVALID_SELECTED_DURATION" or "SKILL_BASE_POINTS_INVALID" => BadRequest(new { result.ErrorCode, result.ErrorMessage }),
            "SKILL_NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
            "SESSION_INVALID_STATUS" or "SESSION_TIME_CONFLICT" => Conflict(new { result.ErrorCode, result.ErrorMessage }),
            _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
        };
    }
}
