using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.Services;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Profile;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;

public class CreateSessionOfferCommandHandler : IRequestHandler<CreateSessionOfferCommand, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISystemConfigService _systemConfigService;
    private readonly ISessionPricingService _sessionPricingService;
    private readonly ISubscriptionEntitlementService _subscriptionEntitlementService;
    private readonly ITransactionExecutor _transactionExecutor;

    public CreateSessionOfferCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ISystemConfigService systemConfigService,
        ISessionPricingService sessionPricingService,
        ISubscriptionEntitlementService subscriptionEntitlementService,
        ITransactionExecutor transactionExecutor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _systemConfigService = systemConfigService;
        _sessionPricingService = sessionPricingService;
        _subscriptionEntitlementService = subscriptionEntitlementService;
        _transactionExecutor = transactionExecutor;
    }

    public async Task<Result<SessionDto>> Handle(CreateSessionOfferCommand request, CancellationToken cancellationToken)
    {
        var companionId = _currentUserService.GetUserId();

        return await _transactionExecutor.ExecuteAsync<SessionDto>(async ct =>
        {
            var companion = await _context.Users
                .Include(item => item.UserProfile)
                .Include(item => item.UserSkills)
                .FirstOrDefaultAsync(item => item.UserId == companionId, ct);
            if (companion == null)
            {
                return Result<SessionDto>.Failure("USER_NOT_FOUND", "User was not found.");
            }

            if (!companion.Roles.Contains("companion"))
            {
                return Result<SessionDto>.Failure("FORBIDDEN", "Only Companion users can create session offers.");
            }

            if (companion.UserProfile == null)
            {
                return Result<SessionDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
            }

            var onboardingState = CompanionOnboardingRules.Evaluate(companion.UserProfile);
            if (!onboardingState.IsComplete)
            {
                return Result<SessionDto>.Failure("COMPANION_PROFILE_INCOMPLETE", "Companion profile is incomplete.");
            }

            var ownsTeachingSkill = companion.UserSkills.Any(item =>
                item.Type == UserSkillType.Teach
                && item.SkillId == request.SkillId);
            if (!ownsTeachingSkill)
            {
                return Result<SessionDto>.Failure("COMPANION_SKILL_NOT_OWNED", "Companion can only create session offers for owned teaching skills.");
            }

            var maxPerDay = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionMaxPerDayPerCompanion, ct);
            var companionEntitlements = await _subscriptionEntitlementService.GetResolvedEntitlementsAsync(companionId, ct);
            if (companionEntitlements.CompanionDailySessionLimitOverride.HasValue)
            {
                maxPerDay = companionEntitlements.CompanionDailySessionLimitOverride.Value;
            }
            var startDay = request.ScheduledAt.Date;
            var endDay = startDay.AddDays(1);
            var existingCount = await _context.Sessions.CountAsync(
                item => item.CompanionId == companionId
                    && item.ScheduledAt >= startDay
                    && item.ScheduledAt < endDay
                    && item.Status != SessionStatus.Cancelled,
                ct);

            if (existingCount >= maxPerDay)
            {
                return Result<SessionDto>.Failure("SESSION_LIMIT_REACHED", "Companion has reached the daily session limit.");
            }

            var skill = await _context.Skills
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.SkillId == request.SkillId, ct);
            if (skill == null || !skill.IsActive || skill.IsDeleted)
            {
                return Result<SessionDto>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
            }

            var durationOptions = SessionPricingService.NormalizeDurations(request.DurationOptions);
            if (durationOptions.Count == 0)
            {
                return Result<SessionDto>.Failure("INVALID_DURATION_OPTIONS", "Duration options are invalid.");
            }

            var bufferMinutes = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionBufferMinutes, ct);
            var reservedDurationMinutes = durationOptions.Max();
            var requestedStart = request.ScheduledAt;
            var requestedEnd = requestedStart.AddMinutes(reservedDurationMinutes);

            var companionSessions = await _context.Sessions
                .Where(item => item.CompanionId == companionId && item.Status != SessionStatus.Cancelled)
                .ToListAsync(ct);
            var hasConflict = companionSessions.Any(existing =>
            {
                var existingEnd = existing.ScheduledAt.AddMinutes(existing.DurationMinutes);
                return requestedStart < existingEnd.AddMinutes(bufferMinutes)
                    && existing.ScheduledAt < requestedEnd.AddMinutes(bufferMinutes);
            });

            if (hasConflict)
            {
                return Result<SessionDto>.Failure("SESSION_TIME_CONFLICT", "Session time conflicts with an existing session.");
            }

            var session = new Session
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                SkillId = skill.SkillId,
                Skill = skill.Name,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                DeliveryMode = SessionDeliveryMode.Online,
                Location = null,
                DurationMinutes = reservedDurationMinutes,
                PricingModel = SessionPricingModel.FormulaV1,
                DurationOptions = durationOptions.ToList(),
                PointCost = 0,
                ScheduledAt = request.ScheduledAt,
                Status = SessionStatus.Available,
                CreatedAt = _dateTimeProvider.UtcNow,
                UpdatedAt = _dateTimeProvider.UtcNow
            };

            var platformMarkupPct = await _sessionPricingService.GetPlatformMarkupPctAsync(ct);
            var previewResult = _sessionPricingService.BuildOfferPreview(
                skill,
                CompanionCredentialRules.GetCredentialCount(companion.UserProfile),
                durationOptions,
                platformMarkupPct);
            if (!previewResult.IsSuccess)
            {
                return Result<SessionDto>.Failure(previewResult.ErrorCode!, previewResult.ErrorMessage!);
            }

            await _context.Sessions.AddAsync(session, ct);
            return Result<SessionDto>.Success(SessionDtoMapper.Map(session, skill, companion.UserProfile, platformMarkupPct));
        }, cancellationToken);
    }
}
