using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
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
    private readonly ITokenLedgerService _tokenLedgerService;
    private readonly ISubscriptionEntitlementService _subscriptionEntitlementService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISystemConfigService _systemConfigService;
    private readonly IAchievementAwardService _achievementAwardService;

    public ConfirmSessionCompletionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPointLedgerService pointLedgerService,
        ITokenLedgerService tokenLedgerService,
        ISubscriptionEntitlementService subscriptionEntitlementService,
        ITransactionExecutor transactionExecutor,
        IDateTimeProvider dateTimeProvider,
        ISystemConfigService systemConfigService,
        IAchievementAwardService achievementAwardService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _pointLedgerService = pointLedgerService;
        _tokenLedgerService = tokenLedgerService;
        _subscriptionEntitlementService = subscriptionEntitlementService;
        _transactionExecutor = transactionExecutor;
        _dateTimeProvider = dateTimeProvider;
        _systemConfigService = systemConfigService;
        _achievementAwardService = achievementAwardService;
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
                return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Hanh dong khong hop le voi trang thai hien tai.");
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

                var learnerChargePoints = session.PricingModel == SessionPricingModel.FormulaV1
                    ? session.LearnerChargePoints ?? 0
                    : session.PointCost;
                if (learnerChargePoints <= 0)
                {
                    return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Session pricing is invalid.");
                }

                int companionAmount;
                int platformAmount;
                if (session.PricingModel == SessionPricingModel.FormulaV1)
                {
                    if (!session.CompanionPayoutPoints.HasValue || !session.PlatformFeePoints.HasValue)
                    {
                        return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Session pricing snapshot is missing.");
                    }

                    companionAmount = session.CompanionPayoutPoints.Value;
                    platformAmount = session.PlatformFeePoints.Value;
                }
                else
                {
                    var platformFeePct = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.PointPlatformFeePct, ct);
                    companionAmount = session.PointCost * (100 - platformFeePct) / 100;
                    platformAmount = session.PointCost - companionAmount;
                }

                var learnerWallet = await _pointLedgerService.GetOrCreateWalletAsync(session.LearnerId.Value, ct);
                var paymentResult = _pointLedgerService.CompleteSessionPayment(
                    learnerWallet,
                    learnerChargePoints,
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

                var tokenResult = await _tokenLedgerService.AwardSessionCompletionTokensAsync(session, ct);
                if (!tokenResult.IsSuccess)
                {
                    return Result<SessionDto>.Failure(tokenResult.ErrorCode!, tokenResult.ErrorMessage!);
                }

                var subscriptionBonusResult = await _subscriptionEntitlementService.ApplyWeeklyCompletionBonusesAsync(session, ct);
                if (!subscriptionBonusResult.IsSuccess)
                {
                    return Result<SessionDto>.Failure(subscriptionBonusResult.ErrorCode!, subscriptionBonusResult.ErrorMessage!);
                }

                session.Status = SessionStatus.Completed;
                session.DisbursedAt = _dateTimeProvider.UtcNow;

                await _achievementAwardService.AwardForCompletedSessionAsync(session, ct);
            }

            session.UpdatedAt = _dateTimeProvider.UtcNow;
            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
