using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using EdSkill.Application.Features.Profile;
using EdSkill.Application.Features.Sessions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionDetail;

public class GetCompanionDetailQueryHandler : IRequestHandler<GetCompanionDetailQuery, Result<CompanionDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISessionPricingService _sessionPricingService;

    public GetCompanionDetailQueryHandler(IApplicationDbContext context, ISessionPricingService sessionPricingService)
    {
        _context = context;
        _sessionPricingService = sessionPricingService;
    }

    public async Task<Result<CompanionDetailDto>> Handle(GetCompanionDetailQuery request, CancellationToken cancellationToken)
    {
        var skill = await _context.Skills
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SkillId == request.SkillId && item.IsActive && !item.IsDeleted, cancellationToken);
        if (skill == null)
        {
            return Result<CompanionDetailDto>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
        }

        var companion = await _context.Users
            .AsNoTracking()
            .Include(user => user.UserProfile)
            .Include(user => user.UserSkills)
            .ThenInclude(userSkill => userSkill.Skill)
            .FirstOrDefaultAsync(user => user.UserId == request.CompanionId, cancellationToken);
        if (companion?.UserProfile == null || !companion.Roles.Contains("companion"))
        {
            return Result<CompanionDetailDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        if (!companion.UserProfile.IsPublic)
        {
            return Result<CompanionDetailDto>.Failure("PROFILE_PRIVATE", "This profile is private.");
        }

        var candidateSessions = (await CompanionDiscoveryMatcher
            .LoadAvailableOnlineSkillSessionsAsync(
                _context.Sessions.AsNoTracking().Where(session => session.CompanionId == request.CompanionId),
                skill,
                cancellationToken))
            .OrderBy(session => session.ScheduledAt)
            .ToList();

        var platformMarkupPct = candidateSessions.Any(session => session.PricingModel == Domain.Enums.SessionPricingModel.FormulaV1)
            ? await _sessionPricingService.GetPlatformMarkupPctAsync(cancellationToken)
            : (int?)null;
        var credentialCount = CompanionCredentialRules.GetCredentialCount(companion.UserProfile);
        var matchedSessions = CompanionDiscoveryMatcher.MatchOffers(
                candidateSessions,
                skill,
                new Dictionary<Guid, Domain.Entities.UserProfile> { [companion.UserId] = companion.UserProfile },
                platformMarkupPct,
                new CompanionDiscoveryFilters(
                    request.MinimumDurationMinutes,
                    request.MaxLearnerChargePoints,
                    request.GetCredentialCountGroup()))
            .Select(item => item.Offer)
            .OrderBy(item => item.ScheduledAt)
            .ThenBy(item => item.PointCost)
            .ToList();

        var reviewBaseQuery =
            from review in _context.Reviews.AsNoTracking()
            join session in _context.Sessions.AsNoTracking() on review.SessionId equals session.SessionId
            where review.RevieweeId == request.CompanionId
                  && session.CompanionId == request.CompanionId
            select review;

        var totalReviews = await reviewBaseQuery.CountAsync(cancellationToken);
        var avgRating = totalReviews == 0
            ? 0d
            : Math.Round(await reviewBaseQuery.AverageAsync(review => review.Rating, cancellationToken), 2);

        var reviewPageItems = await reviewBaseQuery
            .OrderByDescending(review => review.CreatedAt)
            .Skip((request.ReviewPage - 1) * request.ReviewLimit)
            .Take(request.ReviewLimit)
            .ToListAsync(cancellationToken);

        var reviewerIds = reviewPageItems.Select(review => review.ReviewerId).Distinct().ToList();
        var reviewers = await _context.Users
            .AsNoTracking()
            .Include(user => user.UserProfile)
            .Where(user => reviewerIds.Contains(user.UserId))
            .ToListAsync(cancellationToken);

        var reviewerLookup = reviewers.ToDictionary(
            user => user.UserId,
            user => string.IsNullOrWhiteSpace(user.UserProfile?.DisplayName)
                ? user.Username
                : user.UserProfile!.DisplayName);

        var reviewDtos = reviewPageItems
            .Select(review => new CompanionReviewDto(
                review.ReviewId,
                review.Rating,
                review.Comment,
                reviewerLookup.TryGetValue(review.ReviewerId, out var displayName) ? displayName : "Unknown",
                review.CreatedAt))
            .ToList();

        return Result<CompanionDetailDto>.Success(new CompanionDetailDto(
            companion.UserId,
            companion.UserProfile.DisplayName,
            companion.UserProfile.AvatarUrl,
            companion.UserProfile.Bio,
            companion.UserSkills
                .Where(userSkill => userSkill.Type == Domain.Enums.UserSkillType.Teach && userSkill.Skill is not null)
                .Select(userSkill => userSkill.Skill.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            companion.Roles.AsReadOnly(),
            credentialCount,
            companion.UserProfile.TotalSessions,
            companion.UserProfile.LastActiveAt,
            avgRating,
            totalReviews,
            new CompanionReviewListDto(reviewDtos, totalReviews, request.ReviewPage, request.ReviewLimit),
            matchedSessions));
    }
}
