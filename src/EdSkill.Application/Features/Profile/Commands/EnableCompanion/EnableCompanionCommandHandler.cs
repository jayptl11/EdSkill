using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Profile.Commands.EnableCompanion;

public class EnableCompanionCommandHandler : IRequestHandler<EnableCompanionCommand, Result<ProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public EnableCompanionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ProfileDto>> Handle(EnableCompanionCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetUserId();

        var user = await _context.Users
            .Include(item => item.UserProfile)
            .Include(item => item.UserSkills)
            .ThenInclude(item => item.Skill)
            .FirstOrDefaultAsync(item => item.UserId == currentUserId, cancellationToken);

        if (user?.UserProfile == null)
        {
            return Result<ProfileDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        if (!user.Roles.Contains("companion"))
        {
            user.Roles.Add("companion");
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<ProfileDto>.Success(ProfileDtoMapper.Map(user, user.UserProfile));
    }
}
