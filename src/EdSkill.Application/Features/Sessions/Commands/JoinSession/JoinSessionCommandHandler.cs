using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Commands.JoinSession;

public class JoinSessionCommandHandler : IRequestHandler<JoinSessionCommand, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISystemConfigService _systemConfigService;

    public JoinSessionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITransactionExecutor transactionExecutor,
        IDateTimeProvider dateTimeProvider,
        ISystemConfigService systemConfigService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _transactionExecutor = transactionExecutor;
        _dateTimeProvider = dateTimeProvider;
        _systemConfigService = systemConfigService;
    }

    public async Task<Result<SessionDto>> Handle(JoinSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();

        return await _transactionExecutor.ExecuteAsync<SessionDto>(async ct =>
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(item => item.SessionId == request.SessionId, ct);
            if (session == null)
            {
                return Result<SessionDto>.Failure("SESSION_NOT_FOUND", "Session was not found.");
            }

            if (session.CompanionId != userId && session.LearnerId != userId)
            {
                return Result<SessionDto>.Failure("FORBIDDEN", "You do not have access to this session.");
            }

            var joinEarlyMinutes = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionJoinEarlyMinutes, ct);
            var joinLateGraceMinutes = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionJoinLateGraceMinutes, ct);
            var decision = SessionRoomAccessPolicy.Evaluate(session, _dateTimeProvider.UtcNow, joinEarlyMinutes, joinLateGraceMinutes);
            if (!decision.CanJoin)
            {
                return Result<SessionDto>.Failure(decision.DenyCode!, decision.DenyMessage!);
            }

            var existingOpenSegment = await _context.SessionPresenceSegments
                .FirstOrDefaultAsync(
                    item => item.SessionId == session.SessionId && item.UserId == userId && !item.LeftAt.HasValue,
                    ct);

            if (existingOpenSegment == null)
            {
                _context.SessionPresenceSegments.Add(new SessionPresenceSegment
                {
                    SessionId = session.SessionId,
                    UserId = userId,
                    JoinedAt = _dateTimeProvider.UtcNow
                });
            }

            session.ActualStartAt ??= _dateTimeProvider.UtcNow;
            session.Status = Domain.Enums.SessionStatus.InProgress;
            session.UpdatedAt = _dateTimeProvider.UtcNow;

            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
