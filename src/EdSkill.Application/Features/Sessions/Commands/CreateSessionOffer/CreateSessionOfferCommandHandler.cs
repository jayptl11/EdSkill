using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile;
using EdSkill.Application.Features.Sessions;
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
    private readonly ITransactionExecutor _transactionExecutor;

    public CreateSessionOfferCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ISystemConfigService systemConfigService,
        ITransactionExecutor transactionExecutor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _systemConfigService = systemConfigService;
        _transactionExecutor = transactionExecutor;
    }

    public async Task<Result<SessionDto>> Handle(CreateSessionOfferCommand request, CancellationToken cancellationToken)
    {
        var companionId = _currentUserService.GetUserId();

        return await _transactionExecutor.ExecuteAsync<SessionDto>(async ct =>
        {
            var companion = await _context.Users
                .Include(item => item.UserProfile)
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

            var maxPerDay = await _systemConfigService.GetIntValueAsync(Common.System.SystemConfigKeys.SessionMaxPerDayPerCompanion, ct);
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

            var session = new Session
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                Skill = skill.Name,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                DeliveryMode = request.DeliveryMode,
                Location = request.DeliveryMode == SessionDeliveryMode.Offline
                    ? request.Location!.Trim()
                    : null,
                DurationMinutes = request.DurationMinutes,
                PointCost = request.PointCost,
                ScheduledAt = request.ScheduledAt,
                Status = SessionStatus.Available,
                CreatedAt = _dateTimeProvider.UtcNow,
                UpdatedAt = _dateTimeProvider.UtcNow
            };

            await _context.Sessions.AddAsync(session, ct);
            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
