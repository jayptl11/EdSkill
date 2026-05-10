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
