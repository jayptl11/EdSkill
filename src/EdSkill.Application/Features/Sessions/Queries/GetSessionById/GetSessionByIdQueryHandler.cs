using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessionById;

public class GetSessionByIdQueryHandler : IRequestHandler<GetSessionByIdQuery, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISessionPricingService _sessionPricingService;

    public GetSessionByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISessionPricingService sessionPricingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sessionPricingService = sessionPricingService;
    }

    public async Task<Result<SessionDto>> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var session = await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SessionId == request.SessionId, cancellationToken);

        if (session == null)
        {
            return Result<SessionDto>.Failure("SESSION_NOT_FOUND", "Session was not found.");
        }

        var canAccess = session.Status == SessionStatus.Available
            || session.CompanionId == userId
            || session.LearnerId == userId;

        if (!canAccess)
        {
            return Result<SessionDto>.Failure("FORBIDDEN", "You do not have access to this session.");
        }

        Skill? skill = null;
        UserProfile? companionProfile = null;
        int? platformMarkupPct = null;

        if (session.PricingModel == SessionPricingModel.FormulaV1
            && (!session.LearnerChargePoints.HasValue || !session.CompanionPayoutPoints.HasValue || !session.PlatformFeePoints.HasValue))
        {
            if (session.SkillId.HasValue)
            {
                skill = await _context.Skills
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.SkillId == session.SkillId.Value && item.IsActive && !item.IsDeleted, cancellationToken);
            }

            companionProfile = await _context.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == session.CompanionId, cancellationToken);

            if (skill != null && companionProfile != null)
            {
                platformMarkupPct = await _sessionPricingService.GetPlatformMarkupPctAsync(cancellationToken);
            }
        }

        return Result<SessionDto>.Success(SessionDtoMapper.Map(session, skill, companionProfile, platformMarkupPct));
    }
}
