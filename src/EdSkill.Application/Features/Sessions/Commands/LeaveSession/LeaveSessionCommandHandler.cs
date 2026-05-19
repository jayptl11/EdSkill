using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Commands.LeaveSession;

public class LeaveSessionCommandHandler : IRequestHandler<LeaveSessionCommand, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISystemConfigService _systemConfigService;

    public LeaveSessionCommandHandler(
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

    public async Task<Result<SessionDto>> Handle(LeaveSessionCommand request, CancellationToken cancellationToken)
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

            if (session.DeliveryMode != SessionDeliveryMode.Online)
            {
                return Result<SessionDto>.Failure("SESSION_NOT_ONLINE", "Only online sessions support leave tracking.");
            }

            var openSegment = await _context.SessionPresenceSegments
                .OrderByDescending(item => item.JoinedAt)
                .FirstOrDefaultAsync(
                    item => item.SessionId == session.SessionId && item.UserId == userId && !item.LeftAt.HasValue,
                    ct);

            if (openSegment == null)
            {
                if (session.Status is SessionStatus.PendingReview or SessionStatus.Disputed)
                {
                    return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
                }

                return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Session leave is not valid in the current state.");
            }

            openSegment.LeftAt = _dateTimeProvider.UtcNow;
            session.UpdatedAt = _dateTimeProvider.UtcNow;

            var hasOpenParticipant = await _context.SessionPresenceSegments
                .AnyAsync(item => item.SessionId == session.SessionId && !item.LeftAt.HasValue, ct);

            if (hasOpenParticipant)
            {
                session.Status = SessionStatus.InProgress;
                return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
            }

            session.ActualEndAt = _dateTimeProvider.UtcNow;

            var segments = await _context.SessionPresenceSegments
                .Where(item => item.SessionId == session.SessionId)
                .OrderBy(item => item.JoinedAt)
                .ToListAsync(ct);

            var learnerSegments = session.LearnerId.HasValue
                ? segments.Where(item => item.UserId == session.LearnerId.Value).ToList()
                : [];
            var companionSegments = segments.Where(item => item.UserId == session.CompanionId).ToList();

            session.ActualDuration = SessionPresenceDurationCalculator.CalculateSharedMinutes(learnerSegments, companionSegments);

            var minDuration = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionMinDurationMinutes, ct);
            session.Status = session.ActualDuration >= minDuration ? SessionStatus.PendingReview : SessionStatus.Disputed;

            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
