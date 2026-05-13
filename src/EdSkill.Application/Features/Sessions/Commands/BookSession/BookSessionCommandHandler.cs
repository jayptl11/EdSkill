using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Commands.BookSession;

public class BookSessionCommandHandler : IRequestHandler<BookSessionCommand, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPointLedgerService _pointLedgerService;
    private readonly ISessionPricingService _sessionPricingService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BookSessionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPointLedgerService pointLedgerService,
        ISessionPricingService sessionPricingService,
        ITransactionExecutor transactionExecutor,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _pointLedgerService = pointLedgerService;
        _sessionPricingService = sessionPricingService;
        _transactionExecutor = transactionExecutor;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SessionDto>> Handle(BookSessionCommand request, CancellationToken cancellationToken)
    {
        var learnerId = _currentUserService.GetUserId();

        return await _transactionExecutor.ExecuteAsync<SessionDto>(async ct =>
        {
            var learner = await _context.Users.FirstOrDefaultAsync(item => item.UserId == learnerId, ct);
            if (learner == null)
            {
                return Result<SessionDto>.Failure("USER_NOT_FOUND", "User was not found.");
            }

            if (!learner.Roles.Contains("learner"))
            {
                return Result<SessionDto>.Failure("FORBIDDEN", "Only Learner users can book a session.");
            }

            var session = await _context.Sessions.FirstOrDefaultAsync(item => item.SessionId == request.SessionId, ct);
            if (session == null)
            {
                return Result<SessionDto>.Failure("SESSION_NOT_FOUND", "Session was not found.");
            }

            if (session.Status != SessionStatus.Available || session.LearnerId.HasValue)
            {
                return Result<SessionDto>.Failure("SESSION_NOT_AVAILABLE", "Session is not available for booking.");
            }

            if (session.CompanionId == learnerId)
            {
                return Result<SessionDto>.Failure("SELF_BOOKING", "Khong the tu dat phien voi chinh minh.");
            }

            var holdAmount = session.PointCost;
            Skill? skill = null;
            UserProfile? companionProfile = null;
            int? platformMarkupPct = null;

            if (session.PricingModel == SessionPricingModel.FormulaV1)
            {
                if (!session.SkillId.HasValue)
                {
                    return Result<SessionDto>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
                }

                if (!session.DurationOptions.Contains(request.SelectedDurationMinutes))
                {
                    return Result<SessionDto>.Failure("INVALID_SELECTED_DURATION", "Selected duration is not supported for this session.");
                }

                skill = await _context.Skills.FirstOrDefaultAsync(item => item.SkillId == session.SkillId.Value && item.IsActive && !item.IsDeleted, ct);
                if (skill == null)
                {
                    return Result<SessionDto>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
                }

                companionProfile = await _context.UserProfiles.FirstOrDefaultAsync(item => item.UserId == session.CompanionId, ct);
                if (companionProfile == null)
                {
                    return Result<SessionDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
                }

                platformMarkupPct = await _sessionPricingService.GetPlatformMarkupPctAsync(ct);
                var pricingSnapshotResult = _sessionPricingService.BuildBookingSnapshot(
                    skill,
                    CompanionCredentialRules.GetCredentialCount(companionProfile),
                    request.SelectedDurationMinutes,
                    platformMarkupPct.Value);
                if (!pricingSnapshotResult.IsSuccess || pricingSnapshotResult.Value == null)
                {
                    return Result<SessionDto>.Failure(pricingSnapshotResult.ErrorCode!, pricingSnapshotResult.ErrorMessage!);
                }

                session.SelectedDurationMinutes = pricingSnapshotResult.Value.SelectedDurationMinutes;
                session.CompanionPayoutPoints = pricingSnapshotResult.Value.CompanionPayoutPoints;
                session.LearnerChargePoints = pricingSnapshotResult.Value.LearnerChargePoints;
                session.PlatformFeePoints = pricingSnapshotResult.Value.PlatformFeePoints;
                session.SkillBasePointsSnapshot = pricingSnapshotResult.Value.SkillBasePointsSnapshot;
                session.CredentialBonusPointsSnapshot = pricingSnapshotResult.Value.CredentialBonusPointsSnapshot;
                session.DurationMultiplierPercentSnapshot = pricingSnapshotResult.Value.DurationMultiplierPercentSnapshot;
                session.PointCost = pricingSnapshotResult.Value.LearnerChargePoints;
                holdAmount = pricingSnapshotResult.Value.LearnerChargePoints;
            }

            var wallet = await _pointLedgerService.GetOrCreateWalletAsync(learnerId, ct);
            var holdResult = _pointLedgerService.HoldPoints(wallet, holdAmount, session.SessionId, "Points held for session booking.");
            if (!holdResult.IsSuccess)
            {
                return Result<SessionDto>.Failure(holdResult.ErrorCode!, holdResult.ErrorMessage!);
            }

            session.LearnerId = learnerId;
            session.Status = SessionStatus.Pending;
            session.UpdatedAt = _dateTimeProvider.UtcNow;

            return Result<SessionDto>.Success(SessionDtoMapper.Map(session, skill, companionProfile, platformMarkupPct));
        }, cancellationToken);
    }
}
