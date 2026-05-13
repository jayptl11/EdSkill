using EdSkill.Application.Features.Sessions.DTOs;

namespace EdSkill.Application.Features.Companions.DTOs;

public record CompanionSearchItemDto(
    Guid CompanionId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyCollection<string> SkillsToTeach,
    double AvgRating,
    int TotalReviews,
    int MatchingSessionCount,
    int LowestPointCost,
    SessionPricingPreviewDto PricingPreview,
    DateTime NextScheduledAt
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
    int TotalSessions,
    DateTime? LastActiveAt,
    double AvgRating,
    int TotalReviews,
    CompanionReviewListDto Reviews,
    IReadOnlyCollection<SessionDto> Sessions
);
