using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Profile;

public static class CompanionCredentialRules
{
    public static int GetCredentialCount(UserProfile profile)
    {
        var credentialCount = profile.CredentialUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (credentialCount > 0)
        {
            return credentialCount;
        }

        return string.IsNullOrWhiteSpace(profile.DegreeUrl) ? 0 : 1;
    }
}
