namespace EdSkill.Application.Features.Reviews.DTOs;

public record ReviewDto(
    Guid ReviewId,
    Guid SessionId,
    Guid ReviewerId,
    Guid RevieweeId,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

public record CreateReviewRequest(
    Guid SessionId,
    int Rating,
    string? Comment
);

public record ReviewRatingBreakdownDto(
    int Rating,
    int Count
);

public record ReceivedReviewDto(
    Guid ReviewId,
    Guid SessionId,
    int Rating,
    string? Comment,
    string ReviewerDisplayName,
    string? ReviewerAvatarUrl,
    DateTime CreatedAt
);

public record ReviewReceivedSummaryDto(
    double AvgRating,
    int TotalReviews,
    IReadOnlyCollection<ReviewRatingBreakdownDto> RatingBreakdown,
    IReadOnlyCollection<ReceivedReviewDto> RecentReviews
);

public record ReviewTaskDto(
    Guid SessionId,
    Guid RevieweeId,
    string RevieweeDisplayName,
    string? RevieweeAvatarUrl,
    string SkillName,
    int PricePoints,
    string? Description,
    string ReviewStatus,
    ReviewDto? ExistingReview,
    DateTime CompletedAt,
    DateTime ReviewWindowClosesAt
);

public record ReviewDashboardDto(
    ReviewReceivedSummaryDto ReceivedSummary,
    IReadOnlyCollection<ReviewTaskDto> ReviewTasks
);
