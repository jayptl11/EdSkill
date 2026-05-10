using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Commands.ConfirmSessionCompletion;

public class ConfirmSessionCompletionCommandHandler : IRequestHandler<ConfirmSessionCompletionCommand, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPointLedgerService _pointLedgerService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISystemConfigService _systemConfigService;

    public ConfirmSessionCompletionCommandHandler(
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

    public async Task<Result<SessionDto>> Handle(ConfirmSessionCompletionCommand request, CancellationToken cancellationToken)
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

            if (session.Status == SessionStatus.Completed)
            {
                return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
            }

            if (session.Status != SessionStatus.PendingReview)
            {
                return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Hành động không hợp lệ với trạng thái hiện tại.");
            }

            var minDuration = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionMinDurationMinutes, ct);
            if (!session.ActualDuration.HasValue || session.ActualDuration.Value < minDuration)
            {
                return Result<SessionDto>.Failure("SESSION_DURATION_INVALID", "Session duration is below the minimum valid threshold.");
            }

            if (session.LearnerId == userId)
            {
                session.LearnerConfirmed = true;
            }

            if (session.CompanionId == userId)
            {
                session.CompanionConfirmed = true;
            }

            if (session.LearnerConfirmed && session.CompanionConfirmed)
            {
                if (!session.LearnerId.HasValue)
                {
                    return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Session does not have a learner.");
                }

                var platformFeePct = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.PointPlatformFeePct, ct);
                var companionAmount = session.PointCost * (100 - platformFeePct) / 100;
                var platformAmount = session.PointCost - companionAmount;

                var learnerWallet = await _pointLedgerService.GetOrCreateWalletAsync(session.LearnerId.Value, ct);
                var paymentResult = _pointLedgerService.CompleteSessionPayment(
                    learnerWallet,
                    session.PointCost,
                    session.SessionId,
                    "Session completed.");
                if (!paymentResult.IsSuccess)
                {
                    return Result<SessionDto>.Failure(paymentResult.ErrorCode!, paymentResult.ErrorMessage!);
                }

                var companionWallet = await _pointLedgerService.GetOrCreateWalletAsync(session.CompanionId, ct);
                var companionResult = _pointLedgerService.CreditUser(
                    companionWallet,
                    PointTransactionType.SessionEarning,
                    companionAmount,
                    session.SessionId,
                    "Session disbursement.");
                if (!companionResult.IsSuccess)
                {
                    return Result<SessionDto>.Failure(companionResult.ErrorCode!, companionResult.ErrorMessage!);
                }

                var platformLedger = await _pointLedgerService.GetPlatformLedgerAsync(ct);
                var platformResult = _pointLedgerService.CreditPlatform(
                    platformLedger,
                    platformAmount,
                    session.SessionId,
                    "Platform fee after session completion.");
                if (!platformResult.IsSuccess)
                {
                    return Result<SessionDto>.Failure(platformResult.ErrorCode!, platformResult.ErrorMessage!);
                }

                var companionProfile = await _context.UserProfiles.FirstOrDefaultAsync(item => item.UserId == session.CompanionId, ct);
                var learnerProfile = await _context.UserProfiles.FirstOrDefaultAsync(item => item.UserId == session.LearnerId.Value, ct);
                if (companionProfile != null)
                {
                    companionProfile.TotalSessions += 1;
                    companionProfile.UpdatedAt = _dateTimeProvider.UtcNow;
                }

                if (learnerProfile != null)
                {
                    learnerProfile.TotalSessions += 1;
                    learnerProfile.UpdatedAt = _dateTimeProvider.UtcNow;
                }

                session.Status = SessionStatus.Completed;
                session.DisbursedAt = _dateTimeProvider.UtcNow;
            }

            session.UpdatedAt = _dateTimeProvider.UtcNow;
            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
