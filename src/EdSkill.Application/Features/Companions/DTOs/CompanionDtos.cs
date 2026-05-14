using EdSkill.Application.Features.Sessions.DTOs;

namespace EdSkill.Application.Features.Companions.DTOs;

public sealed class SearchCompanionsRequest
{
    public Guid SkillId { get; init; }
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
    IReadOnlyCollection<SessionDto> MatchedOffers
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
