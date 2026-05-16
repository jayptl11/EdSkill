using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionPublicProfile;

public class GetCompanionPublicProfileQueryHandler : IRequestHandler<GetCompanionPublicProfileQuery, Result<CompanionPublicProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISessionPricingService _sessionPricingService;

    public GetCompanionPublicProfileQueryHandler(IApplicationDbContext context, ISessionPricingService sessionPricingService)
    {
        _context = context;
        _sessionPricingService = sessionPricingService;
    }

    public async Task<Result<CompanionPublicProfileDto>> Handle(GetCompanionPublicProfileQuery request, CancellationToken cancellationToken)
    {
        var companion = await _context.Users
            .AsNoTracking()
            .Include(user => user.UserProfile)
            .Include(user => user.UserSkills)
            .ThenInclude(userSkill => userSkill.Skill)
            .FirstOrDefaultAsync(user => user.UserId == request.CompanionId, cancellationToken);
        if (companion?.UserProfile == null || !companion.Roles.Contains("companion"))
        {
            return Result<CompanionPublicProfileDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        if (!companion.UserProfile.IsPublic)
        {
            return Result<CompanionPublicProfileDto>.Failure("PROFILE_PRIVATE", "This profile is private.");
        }

        var teachSkills = companion.UserSkills
            .Where(userSkill => userSkill.Type == Domain.Enums.UserSkillType.Teach && userSkill.Skill is not null)
            .Select(userSkill => userSkill.Skill)
            .GroupBy(skill => skill.SkillId)
            .Select(group => group.First())
            .ToList();

        var availableOffersBySkillId = new Dictionary<Guid, List<EdSkill.Application.Features.Sessions.DTOs.SessionDto>>();
        foreach (var skill in teachSkills)
        {
            availableOffersBySkillId[skill.SkillId] =
                await CompanionProfileDataLoader.LoadSkillOffersAsync(_context, _sessionPricingService, companion, skill, cancellationToken);
        }

        var achievements = await CompanionProfileDataLoader.LoadAchievementsAsync(_context, request.CompanionId, cancellationToken);
        var reviewData = await CompanionProfileDataLoader.LoadReviewsAsync(_context, request.CompanionId, 1, 1, cancellationToken);
        var totalTeachingMinutes = await _context.Sessions
            .AsNoTracking()
            .Where(session => session.CompanionId == request.CompanionId && session.Status == Domain.Enums.SessionStatus.Completed)
            .SumAsync(session => session.ActualDuration ?? 0, cancellationToken);

        return Result<CompanionPublicProfileDto>.Success(new CompanionPublicProfileDto(
            companion.UserId,
            companion.UserProfile.DisplayName,
            companion.UserProfile.AvatarUrl,
            companion.UserProfile.Bio,
            companion.Roles.AsReadOnly(),
            new CompanionActivitySummaryDto(
                companion.UserProfile.TotalSessions,
                totalTeachingMinutes / 60,
                reviewData.AvgRating,
                reviewData.TotalReviews,
                companion.UserProfile.LastActiveAt),
            achievements,
            CompanionProfileDataLoader.BuildTeachingSkills(companion, availableOffersBySkillId)));
    }
}
