using EdSkill.Domain.Enums;

namespace EdSkill.Application.Common.Policies;

public static class PolicyTypeMapper
{
    public static readonly string[] RequiredRegistrationPolicyTypes =
    [
        "terms",
        "privacy",
        "points_tokens"
    ];

    public static bool TryParse(string? value, out PolicyType policyType)
    {
        switch (Normalize(value))
        {
            case "terms":
                policyType = PolicyType.Terms;
                return true;
            case "privacy":
                policyType = PolicyType.Privacy;
                return true;
            case "points_tokens":
                policyType = PolicyType.PointsTokens;
                return true;
            case "community_guidelines":
                policyType = PolicyType.CommunityGuidelines;
                return true;
            default:
                policyType = default;
                return false;
        }
    }

    public static string ToApiValue(PolicyType policyType) => policyType switch
    {
        PolicyType.Terms => "terms",
        PolicyType.Privacy => "privacy",
        PolicyType.PointsTokens => "points_tokens",
        PolicyType.CommunityGuidelines => "community_guidelines",
        _ => throw new ArgumentOutOfRangeException(nameof(policyType), policyType, null)
    };

    public static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
}
