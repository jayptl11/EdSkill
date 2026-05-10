using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Profile;

internal static class CompanionOnboardingRules
{
    public static CompanionOnboardingState Evaluate(UserProfile profile)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            missingFields.Add("displayName");
        }

        if (string.IsNullOrWhiteSpace(profile.AvatarUrl))
        {
            missingFields.Add("avatarUrl");
        }

        if (string.IsNullOrWhiteSpace(profile.Bio))
        {
            missingFields.Add("bio");
        }

        if (string.IsNullOrWhiteSpace(profile.University))
        {
            missingFields.Add("university");
        }

        if (string.IsNullOrWhiteSpace(profile.Faculty))
        {
            missingFields.Add("faculty");
        }

        if (!profile.YearOfStudy.HasValue)
        {
            missingFields.Add("yearOfStudy");
        }

        if (profile.SkillsToTeach is not { Count: > 0 })
        {
            missingFields.Add("skillsToTeach");
        }

        if (!profile.IsPublic)
        {
            missingFields.Add("isPublic");
        }

        return new CompanionOnboardingState(missingFields.Count == 0, missingFields);
    }
}

internal sealed record CompanionOnboardingState(
    bool IsComplete,
    IReadOnlyCollection<string> MissingFields);
