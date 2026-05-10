using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Commands.CancelSession;

public class CancelSessionCommandHandler : IRequestHandler<CancelSessionCommand, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPointLedgerService _pointLedgerService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISystemConfigService _systemConfigService;

    public CancelSessionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPointLedgerService pointLedgerService,
        ITransactionExecutor transactionExecutor,
        IDateTimeProvider dateTimeProvider,
        ISystemConfigService systemConfigService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _pointLedgerService = pointLedgerService;
        _transactionExecutor = transactionExecutor;
        _dateTimeProvider = dateTimeProvider;
        _systemConfigService = systemConfigService;
    }

    public async Task<Result<SessionDto>> Handle(CancelSessionCommand request, CancellationToken cancellationToken)
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

            if (session.Status is not (SessionStatus.Pending or SessionStatus.Confirmed))
            {
                return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Hành động không hợp lệ với trạng thái hiện tại.");
            }

            if (!session.LearnerId.HasValue)
            {
                return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Session has not been booked yet.");
            }

            var learnerWallet = await _pointLedgerService.GetOrCreateWalletAsync(session.LearnerId.Value, ct);

            if (session.CompanionId == userId)
            {
                var companionCancelRefundResult = _pointLedgerService.ReleaseHeldPoints(
                    learnerWallet,
                    session.PointCost,
                    session.SessionId,
                    PointTransactionType.Refund,
                    "Points refunded after Companion cancellation.");

                if (!companionCancelRefundResult.IsSuccess)
                {
                    return Result<SessionDto>.Failure(companionCancelRefundResult.ErrorCode!, companionCancelRefundResult.ErrorMessage!);
                }
            }
            else
            {
                var cancelDeadlineHours = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionCancelDeadlineHours, ct);
                var isLateCancel = session.Status == SessionStatus.Confirmed
                    && _dateTimeProvider.UtcNow > session.ScheduledAt.AddHours(-cancelDeadlineHours);

                if (!isLateCancel || session.Status == SessionStatus.Pending)
                {
                    var refundResult = _pointLedgerService.ReleaseHeldPoints(
                        learnerWallet,
                        session.PointCost,
                        session.SessionId,
                        PointTransactionType.Refund,
                        "Points refunded after Learner cancellation.");

                    if (!refundResult.IsSuccess)
                    {
                        return Result<SessionDto>.Failure(refundResult.ErrorCode!, refundResult.ErrorMessage!);
                    }
                }
                else
                {
                    var companionPct = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionLateCancelCompanionPct, ct);
                    var platformPct = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionLateCancelPlatformPct, ct);

                    var paymentResult = _pointLedgerService.CompleteSessionPayment(
                        learnerWallet,
                        session.PointCost,
                        session.SessionId,
                        "cancelled_no_refund");
                    if (!paymentResult.IsSuccess)
                    {
                        return Result<SessionDto>.Failure(paymentResult.ErrorCode!, paymentResult.ErrorMessage!);
                    }

                    var companionWallet = await _pointLedgerService.GetOrCreateWalletAsync(session.CompanionId, ct);
                    var companionAmount = session.PointCost * companionPct / 100;
                    var platformAmount = session.PointCost * platformPct / 100;

                    var companionResult = _pointLedgerService.CreditUser(
                        companionWallet,
                        PointTransactionType.SessionEarning,
                        companionAmount,
                        session.SessionId,
                        "Late cancel compensation.");
                    if (!companionResult.IsSuccess)
                    {
                        return Result<SessionDto>.Failure(companionResult.ErrorCode!, companionResult.ErrorMessage!);
                    }

                    var platformLedger = await _pointLedgerService.GetPlatformLedgerAsync(ct);
                    var platformResult = _pointLedgerService.CreditPlatform(
                        platformLedger,
                        platformAmount,
                        session.SessionId,
                        "Late cancel platform fee.");
                    if (!platformResult.IsSuccess)
                    {
                        return Result<SessionDto>.Failure(platformResult.ErrorCode!, platformResult.ErrorMessage!);
                    }
                }
            }

            session.Status = SessionStatus.Cancelled;
            session.CancelReason = request.Reason;
            session.CancelledBy = userId;
            session.CancelledAt = _dateTimeProvider.UtcNow;
            session.UpdatedAt = _dateTimeProvider.UtcNow;

            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
