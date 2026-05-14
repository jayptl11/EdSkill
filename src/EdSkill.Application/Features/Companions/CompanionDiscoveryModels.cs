namespace EdSkill.Application.Features.Companions;

internal enum CompanionCredentialCountGroup
{
    Zero = 0,
    One = 1,
    Two = 2,
    ThreeOrMore = 3
}

internal sealed record CompanionDiscoveryFilters(
    int? MinimumDurationMinutes,
    int? MaxLearnerChargePoints,
    CompanionCredentialCountGroup? CredentialCountGroup);

internal sealed record MatchedCompanionOffer(
    Guid CompanionId,
    int CredentialCount,
    EdSkill.Application.Features.Sessions.DTOs.SessionDto Offer);

internal static class CompanionCredentialCountGroupParser
{
    public static bool IsValid(string value)
    {
        return Parse(value).HasValue;
    }

    public static CompanionCredentialCountGroup? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "zero" => CompanionCredentialCountGroup.Zero,
            "one" => CompanionCredentialCountGroup.One,
            "two" => CompanionCredentialCountGroup.Two,
            "threeormore" => CompanionCredentialCountGroup.ThreeOrMore,
            _ => null
        };
    }

    public static bool Matches(CompanionCredentialCountGroup? group, int credentialCount)
    {
        return group switch
        {
            null => true,
            CompanionCredentialCountGroup.Zero => credentialCount == 0,
            CompanionCredentialCountGroup.One => credentialCount == 1,
            CompanionCredentialCountGroup.Two => credentialCount == 2,
            CompanionCredentialCountGroup.ThreeOrMore => credentialCount >= 3,
            _ => false
        };
    }
}
