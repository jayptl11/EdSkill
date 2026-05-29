using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionSkillDetail;

public class GetCompanionSkillDetailQueryHandler : IRequestHandler<GetCompanionSkillDetailQuery, Result<CompanionSkillDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISessionPricingService _sessionPricingService;

    public GetCompanionSkillDetailQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ISessionPricingService sessionPricingService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _sessionPricingService = sessionPricingService;
    }

    public async Task<Result<CompanionSkillDetailDto>> Handle(GetCompanionSkillDetailQuery request, CancellationToken cancellationToken)
    {
        var companion = await _context.Users
            .AsNoTracking()
            .Include(user => user.UserProfile)
            .Include(user => user.UserSkills)
            .ThenInclude(userSkill => userSkill.Skill)
            .FirstOrDefaultAsync(user => user.UserId == request.CompanionId, cancellationToken);
        if (companion?.UserProfile == null || !companion.Roles.Contains("companion"))
        {
            return Result<CompanionSkillDetailDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        if (!companion.UserProfile.IsPublic)
        {
            return Result<CompanionSkillDetailDto>.Failure("PROFILE_PRIVATE", "This profile is private.");
        }

        var skill = companion.UserSkills
            .Where(userSkill => userSkill.Type == Domain.Enums.UserSkillType.Teach && userSkill.SkillId == request.SkillId && userSkill.Skill is not null)
            .Select(userSkill => userSkill.Skill)
            .FirstOrDefault();
        if (skill == null || !skill.IsActive || skill.IsDeleted)
        {
            return Result<CompanionSkillDetailDto>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
        }

        var offers = await CompanionProfileDataLoader.LoadSkillOffersAsync(
            _context,
            _sessionPricingService,
            companion,
            skill,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        var totalOffers = offers.Count;
        var pagedOffers = offers
            .Skip((request.OfferPage - 1) * request.OfferLimit)
            .Take(request.OfferLimit)
            .ToList();

        var reviewData = await CompanionProfileDataLoader.LoadReviewsAsync(_context, request.CompanionId, request.ReviewPage, request.ReviewLimit, cancellationToken);

        return Result<CompanionSkillDetailDto>.Success(new CompanionSkillDetailDto(
            companion.UserId,
            new CompanionSkillInfoDto(skill.SkillId, skill.Name, skill.IconKey),
            reviewData.AvgRating,
            reviewData.TotalReviews,
            new SessionListDto(pagedOffers, totalOffers, request.OfferPage, request.OfferLimit),
            new CompanionReviewListDto(reviewData.Reviews, reviewData.TotalReviews, request.ReviewPage, request.ReviewLimit)));
    }
}
