using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Application.Features.Profile.DTOs;
using EdSkill.Application.Features.Subscriptions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.Profile;

internal static class ProfileDtoMapper
{
    public static ProfileDto Map(
        User user,
        UserProfile profile,
        IReadOnlyCollection<AchievementSummaryDto>? achievements = null,
        bool includePrivateDetails = true,
        IReadOnlyCollection<ActiveSubscriptionSummaryDto>? activeSubscriptions = null,
        ResolvedSubscriptionEntitlementsDto? subscriptionEntitlements = null)
    {
        var teachingSkills = user.UserSkills
            .Where(userSkill => userSkill.Type == UserSkillType.Teach && userSkill.Skill is not null)
            .Select(userSkill => userSkill.Skill)
            .GroupBy(skill => skill.SkillId)
            .Select(group => group.First())
            .OrderBy(skill => skill.Name)
            .Select(skill => new ProfileSkillDto(skill.SkillId, skill.Name, skill.IconKey))
            .ToList();

        var learningSkills = user.UserSkills
            .Where(userSkill => userSkill.Type == UserSkillType.Learn && userSkill.Skill is not null)
            .Select(userSkill => userSkill.Skill)
            .GroupBy(skill => skill.SkillId)
            .Select(group => group.First())
            .OrderBy(skill => skill.Name)
            .Select(skill => new ProfileSkillDto(skill.SkillId, skill.Name, skill.IconKey))
            .ToList();

        var skillsToTeach = teachingSkills
            .Select(skill => skill.Name)
            .ToList();

        var skillsToLearn = learningSkills
            .Select(skill => skill.Name)
            .ToList();

        profile.SkillsToTeach = skillsToTeach;
        profile.SkillsToLearn = skillsToLearn;
        var onboardingState = CompanionOnboardingRules.Evaluate(profile);

        return new ProfileDto(
            user.UserId,
            user.Email,
            profile.DisplayName,
            profile.AvatarUrl,
            profile.Bio,
            includePrivateDetails ? profile.DateOfBirth : null,
            includePrivateDetails ? profile.Phone : null,
            includePrivateDetails ? profile.Gender : null,
            includePrivateDetails ? profile.SocialLinkUrl : null,
            includePrivateDetails ? profile.DegreeUrl : null,
            includePrivateDetails ? profile.CredentialUrls.AsReadOnly() : Array.Empty<string>(),
            profile.CredentialUrls.Count,
            includePrivateDetails ? profile.Address : null,
            skillsToTeach.AsReadOnly(),
            skillsToLearn.AsReadOnly(),
            teachingSkills.AsReadOnly(),
            learningSkills.AsReadOnly(),
            achievements ?? Array.Empty<AchievementSummaryDto>(),
            profile.IsPublic,
            user.Roles.AsReadOnly(),
            profile.TotalSessions,
            profile.LastActiveAt,
            onboardingState.IsComplete,
            onboardingState.MissingFields,
            activeSubscriptions ?? Array.Empty<ActiveSubscriptionSummaryDto>(),
            subscriptionEntitlements);
    }
}
