using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Profile.Queries.GetMyProfile;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Result<ProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyProfileQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetUserId();

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserProfile)
            .Include(u => u.UserSkills)
            .ThenInclude(us => us.Skill)
            .FirstOrDefaultAsync(u => u.UserId == currentUserId, cancellationToken);

        if (user?.UserProfile == null)
        {
            return Result<ProfileDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        var achievements = await CompanionProfileDataLoader.LoadAchievementsAsync(_context, currentUserId, cancellationToken);
        return Result<ProfileDto>.Success(ProfileDtoMapper.Map(user, user.UserProfile, achievements));
    }
}
