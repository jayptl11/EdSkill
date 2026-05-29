using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Application.Features.Sessions.DTOs;

namespace EdSkill.Application.Features.Companions.DTOs;

public sealed class SearchCompanionsRequest
{
    public Guid? SkillId { get; init; }
    public int? MinimumDurationMinutes { get; init; }
    public int? MaxLearnerChargePoints { get; init; }
    public string? CredentialCountGroup { get; init; }
    public string? DeliveryMode { get; init; }
    public string? Location { get; init; }
    public int Page { get; init; } = 1;
    public int Limit { get; init; } = 20;
}

public sealed class GetCompanionDetailRequest
{
    public Guid SkillId { get; init; }
    public int? MinimumDurationMinutes { get; init; }
    public int? MaxLearnerChargePoints { get; init; }
    public string? CredentialCountGroup { get; init; }
    public string? DeliveryMode { get; init; }
    public string? Location { get; init; }
    public int ReviewPage { get; init; } = 1;
    public int ReviewLimit { get; init; } = 10;
}

public sealed class GetCompanionPublicProfileRequest
{
}

public sealed class GetCompanionSkillDetailRequest
{
    public int ReviewPage { get; init; } = 1;
    public int ReviewLimit { get; init; } = 10;
    public int OfferPage { get; init; } = 1;
    public int OfferLimit { get; init; } = 20;
}

public record CompanionSearchItemDto(
    Guid CompanionId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyCollection<string> SkillsToTeach,
    int CredentialCount,
    double AvgRating,
    int TotalReviews,
    int MatchingSessionCount,
    int LowestPointCost,
    SessionPricingPreviewDto PricingPreview,
    DateTime NextScheduledAt,
    IReadOnlyCollection<SessionDto> MatchedOffers,
    string? SubscriptionBadge,
    bool HasPriorityVisibility
);

public record CompanionSearchResultDto(
    IReadOnlyCollection<CompanionSearchItemDto> Data,
    int Total,
    int Page,
    int Limit
);

public record CompanionReviewDto(
    Guid ReviewId,
    int Rating,
    string? Comment,
    string ReviewerDisplayName,
    DateTime CreatedAt
);

public record CompanionReviewListDto(
    IReadOnlyCollection<CompanionReviewDto> Data,
    int Total,
    int Page,
    int Limit
);

public record CompanionDetailDto(
    Guid CompanionId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyCollection<string> SkillsToTeach,
    IReadOnlyCollection<string> Roles,
    int CredentialCount,
    int TotalSessions,
    DateTime? LastActiveAt,
    double AvgRating,
    int TotalReviews,
    CompanionReviewListDto Reviews,
    IReadOnlyCollection<SessionDto> Sessions
);

public record CompanionActivitySummaryDto(
    int TotalSessions,
    int TotalTeachingHours,
    double AvgRating,
    int TotalReviews,
    DateTime? LastActiveAt
);

public record CompanionTeachingSkillDto(
    Guid SkillId,
    string Name,
    string? IconKey,
    int OfferCount,
    int? StartingPointCost,
    DateTime? NextScheduledAt,
    bool HasAvailableOffers
);

public record CompanionPublicProfileDto(
    Guid CompanionId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyCollection<string> Roles,
    CompanionActivitySummaryDto ActivitySummary,
    IReadOnlyCollection<AchievementSummaryDto> Achievements,
    IReadOnlyCollection<CompanionTeachingSkillDto> TeachingSkills,
    string? SubscriptionBadge,
    bool HasPriorityVisibility
);

public record CompanionSkillInfoDto(
    Guid SkillId,
    string Name,
    string? IconKey
);

public record CompanionSkillDetailDto(
    Guid CompanionId,
    CompanionSkillInfoDto Skill,
    double AvgRating,
    int TotalReviews,
    SessionListDto Offers,
    CompanionReviewListDto Reviews
);
