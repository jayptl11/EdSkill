using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Profile.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<ProfileDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserProfile)
            .Include(u => u.UserSkills)
            .ThenInclude(us => us.Skill)
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

        if (user?.UserProfile == null)
        {
            return Result<ProfileDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        if (!user.UserProfile.IsPublic)
        {
            return Result<ProfileDto>.Failure("PROFILE_PRIVATE", "This profile is private.");
        }

        var achievements = await CompanionProfileDataLoader.LoadAchievementsAsync(_context, user.UserId, cancellationToken);
        return Result<ProfileDto>.Success(ProfileDtoMapper.Map(user, user.UserProfile, achievements, includePrivateDetails: false));
    }
}
