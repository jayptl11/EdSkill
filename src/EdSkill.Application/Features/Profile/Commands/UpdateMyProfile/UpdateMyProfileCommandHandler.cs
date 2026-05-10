using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.DTOs;
using EdSkill.Application.Features.Skills;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
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
            .Include(u => u.UserSkills)
            .ThenInclude(us => us.Skill)
            .FirstOrDefaultAsync(u => u.UserId == currentUserId, cancellationToken);

        if (user?.UserProfile == null)
        {
            return Result<ProfileDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        var profile = user.UserProfile;
        List<Skill>? activeSkills = null;

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
            activeSkills ??= await _context.Skills
                .ToListAsync(cancellationToken);

            var teachSkillsResult = ResolveSkills(request.SkillsToTeach, activeSkills);
            if (teachSkillsResult.IsFailure)
            {
                return Result<ProfileDto>.Failure(teachSkillsResult.ErrorCode!, teachSkillsResult.ErrorMessage!);
            }

            ReplaceUserSkills(user, UserSkillType.Teach, teachSkillsResult.Value!);
            profile.SkillsToTeach = teachSkillsResult.Value!.Select(skill => skill.Name).ToList();
        }

        if (request.HasSkillsToLearn)
        {
            activeSkills ??= await _context.Skills
                .ToListAsync(cancellationToken);

            var learnSkillsResult = ResolveSkills(request.SkillsToLearn, activeSkills);
            if (learnSkillsResult.IsFailure)
            {
                return Result<ProfileDto>.Failure(learnSkillsResult.ErrorCode!, learnSkillsResult.ErrorMessage!);
            }

            ReplaceUserSkills(user, UserSkillType.Learn, learnSkillsResult.Value!);
            profile.SkillsToLearn = learnSkillsResult.Value!.Select(skill => skill.Name).ToList();
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

    private static Result<List<Skill>> ResolveSkills(IReadOnlyCollection<string>? skills, IReadOnlyCollection<Skill> activeSkills)
    {
        if (skills is null || skills.Count == 0)
        {
            return Result<List<Skill>>.Success(new List<Skill>());
        }

        var lookup = SkillNormalization.BuildLookup(activeSkills);
        var resolvedSkills = new List<Skill>();
        var seenSkillIds = new HashSet<Guid>();

        foreach (var input in skills)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Result<List<Skill>>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
            }

            var normalizedInput = SkillNormalization.NormalizeLookup(input);
            if (!lookup.TryGetValue(normalizedInput, out var resolvedSkill))
            {
                return Result<List<Skill>>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
            }

            if (!resolvedSkill.IsActive)
            {
                return Result<List<Skill>>.Failure("SKILL_INACTIVE", "Skill is inactive.");
            }

            if (!seenSkillIds.Add(resolvedSkill.SkillId))
            {
                return Result<List<Skill>>.Failure("DUPLICATE_SKILL_SELECTION", "Duplicate skill selection is not allowed.");
            }

            resolvedSkills.Add(resolvedSkill);
        }

        return Result<List<Skill>>.Success(resolvedSkills);
    }

    private void ReplaceUserSkills(User user, UserSkillType type, IReadOnlyCollection<Skill> skills)
    {
        var existingEntries = user.UserSkills
            .Where(userSkill => userSkill.Type == type)
            .ToList();

        if (existingEntries.Count > 0)
        {
            _context.UserSkills.RemoveRange(existingEntries);
            foreach (var existingEntry in existingEntries)
            {
                if (user.UserSkills.Contains(existingEntry))
                {
                    user.UserSkills.Remove(existingEntry);
                }
            }
        }

        var newEntries = skills
            .Select(skill => new UserSkill
            {
                UserSkillId = Guid.NewGuid(),
                UserId = user.UserId,
                User = user,
                SkillId = skill.SkillId,
                Skill = skill,
                Type = type,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (newEntries.Count > 0)
        {
            _context.UserSkills.AddRange(newEntries);
            foreach (var newEntry in newEntries)
            {
                if (!user.UserSkills.Contains(newEntry))
                {
                    user.UserSkills.Add(newEntry);
                }
            }
        }
    }
}
