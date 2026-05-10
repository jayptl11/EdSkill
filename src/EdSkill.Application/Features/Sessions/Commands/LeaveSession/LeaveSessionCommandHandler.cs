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

            if (session.Status != SessionStatus.InProgress)
            {
                return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Hành động không hợp lệ với trạng thái hiện tại.");
            }

            if (session.DeliveryMode != SessionDeliveryMode.Online)
            {
                return Result<SessionDto>.Failure("SESSION_NOT_ONLINE", "Only online sessions support leave tracking.");
            }

            session.ActualEndAt = _dateTimeProvider.UtcNow;
            var duration = request.ActualDuration
                ?? (session.ActualStartAt.HasValue
                    ? Math.Max(0, (int)Math.Round((session.ActualEndAt.Value - session.ActualStartAt.Value).TotalMinutes))
                    : 0);

            session.ActualDuration = duration;

            var minDuration = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionMinDurationMinutes, ct);
            session.Status = duration >= minDuration ? SessionStatus.PendingReview : SessionStatus.Disputed;
            session.UpdatedAt = _dateTimeProvider.UtcNow;

            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
