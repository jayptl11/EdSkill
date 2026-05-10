using EdSkill.Application.Features.Profile.DTOs;
using EdSkill.Domain.Enums;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Profile;

internal static class ProfileDtoMapper
{
    public static ProfileDto Map(User user, UserProfile profile)
    {
        var skillsToTeach = user.UserSkills
            .Where(userSkill => userSkill.Type == UserSkillType.Teach && userSkill.Skill is not null)
            .Select(userSkill => userSkill.Skill.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        var skillsToLearn = user.UserSkills
            .Where(userSkill => userSkill.Type == UserSkillType.Learn && userSkill.Skill is not null)
            .Select(userSkill => userSkill.Skill.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        profile.SkillsToTeach = skillsToTeach;
        profile.SkillsToLearn = skillsToLearn;
        var onboardingState = CompanionOnboardingRules.Evaluate(profile);

        return new ProfileDto(
            user.UserId,
            profile.DisplayName,
            profile.AvatarUrl,
            profile.Bio,
            profile.University,
            profile.Faculty,
            profile.YearOfStudy,
            skillsToTeach.AsReadOnly(),
            skillsToLearn.AsReadOnly(),
            profile.IsPublic,
            user.Roles.AsReadOnly(),
            profile.TotalSessions,
            profile.LastActiveAt,
            onboardingState.IsComplete,
            onboardingState.MissingFields);
    }
}
