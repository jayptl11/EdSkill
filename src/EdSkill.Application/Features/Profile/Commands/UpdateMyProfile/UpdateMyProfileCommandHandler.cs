using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Profile.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, Result<ProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMyProfileCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ProfileDto>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetUserId();

        var user = await _context.Users
            .Include(u => u.UserProfile)
            .FirstOrDefaultAsync(u => u.UserId == currentUserId, cancellationToken);

        if (user?.UserProfile == null)
        {
            return Result<ProfileDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        var profile = user.UserProfile;

        if (request.HasDisplayName)
        {
            profile.DisplayName = request.DisplayName!.Trim();
        }

        if (request.HasBio)
        {
            profile.Bio = NormalizeOptionalString(request.Bio);
        }

        if (request.HasUniversity)
        {
            profile.University = NormalizeOptionalString(request.University);
        }

        if (request.HasFaculty)
        {
            profile.Faculty = NormalizeOptionalString(request.Faculty);
        }

        if (request.HasYearOfStudy)
        {
            profile.YearOfStudy = request.YearOfStudy;
        }

        if (request.HasSkillsToTeach)
        {
            profile.SkillsToTeach = NormalizeSkills(request.SkillsToTeach);
        }

        if (request.HasSkillsToLearn)
        {
            profile.SkillsToLearn = NormalizeSkills(request.SkillsToLearn);
        }

        if (request.HasAvatarUrl)
        {
            profile.AvatarUrl = NormalizeOptionalString(request.AvatarUrl);
        }

        if (request.HasIsPublic)
        {
            profile.IsPublic = request.IsPublic ?? profile.IsPublic;
        }

        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ProfileDto>.Success(ProfileDtoMapper.Map(user, profile));
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static List<string> NormalizeSkills(IReadOnlyCollection<string>? skills)
    {
        if (skills is null || skills.Count == 0)
        {
            return new List<string>();
        }

        return skills
            .Select(skill => skill.Trim())
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
