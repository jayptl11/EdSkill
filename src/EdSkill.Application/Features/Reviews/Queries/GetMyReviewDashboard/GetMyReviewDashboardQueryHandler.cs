using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Reviews.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Reviews.Queries.GetMyReviewDashboard;

public class GetMyReviewDashboardQueryHandler : IRequestHandler<GetMyReviewDashboardQuery, Result<ReviewDashboardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetMyReviewDashboardQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ReviewDashboardDto>> Handle(GetMyReviewDashboardQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetUserId();
        var receivedSummary = await BuildReceivedSummaryAsync(currentUserId, cancellationToken);
        var reviewTasks = await BuildReviewTasksAsync(currentUserId, cancellationToken);

        return Result<ReviewDashboardDto>.Success(new ReviewDashboardDto(receivedSummary, reviewTasks));
    }

    private async Task<ReviewReceivedSummaryDto> BuildReceivedSummaryAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var receivedBaseQuery =
            from review in _context.Reviews.AsNoTracking()
            join session in _context.Sessions.AsNoTracking() on review.SessionId equals session.SessionId
            where review.RevieweeId == currentUserId && session.CompanionId == currentUserId
            select review;

        var totalReviews = await receivedBaseQuery.CountAsync(cancellationToken);
        var avgRating = totalReviews == 0
            ? 0d
            : Math.Round(await receivedBaseQuery.AverageAsync(item => item.Rating, cancellationToken), 2);

        var ratingCounts = totalReviews == 0
            ? []
            : await receivedBaseQuery
                .GroupBy(item => item.Rating)
                .Select(group => new { Rating = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

        var breakdown = Enumerable.Range(1, 5)
            .Reverse()
            .Select(rating => new ReviewRatingBreakdownDto(
                rating,
                ratingCounts.FirstOrDefault(item => item.Rating == rating)?.Count ?? 0))
            .ToList();

        var recentReviews = await receivedBaseQuery
            .OrderByDescending(item => item.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var reviewerIds = recentReviews
            .Select(item => item.ReviewerId)
            .Distinct()
            .ToList();

        var reviewers = await _context.Users
            .AsNoTracking()
            .Include(item => item.UserProfile)
            .Where(item => reviewerIds.Contains(item.UserId))
            .ToListAsync(cancellationToken);

        var reviewerLookup = reviewers.ToDictionary(
            item => item.UserId,
            item => (
                DisplayName: string.IsNullOrWhiteSpace(item.UserProfile?.DisplayName) ? item.Username : item.UserProfile!.DisplayName,
                AvatarUrl: item.UserProfile?.AvatarUrl));

        var recentReviewDtos = recentReviews
            .Select(item =>
            {
                reviewerLookup.TryGetValue(item.ReviewerId, out var reviewer);
                return new ReceivedReviewDto(
                    item.ReviewId,
                    item.SessionId,
                    item.Rating,
                    item.Comment,
                    reviewer.DisplayName ?? "Unknown",
                    reviewer.AvatarUrl,
                    item.CreatedAt);
            })
            .ToList();

        return new ReviewReceivedSummaryDto(avgRating, totalReviews, breakdown, recentReviewDtos);
    }

    private async Task<IReadOnlyCollection<ReviewTaskDto>> BuildReviewTasksAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var sessions = await _context.Sessions
            .AsNoTracking()
            .Where(item =>
                item.Status == SessionStatus.Completed &&
                (item.CompanionId == currentUserId || item.LearnerId == currentUserId))
            .OrderByDescending(item => item.DisbursedAt ?? item.UpdatedAt)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return [];
        }

        var sessionIds = sessions.Select(item => item.SessionId).ToList();
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(item => sessionIds.Contains(item.SessionId) && item.ReviewerId == currentUserId)
            .ToListAsync(cancellationToken);

        var reviewLookup = reviews.ToDictionary(item => item.SessionId);

        var revieweeIds = sessions
            .Select(item => item.CompanionId == currentUserId ? item.LearnerId : item.CompanionId)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToList();

        var reviewees = await _context.Users
            .AsNoTracking()
            .Include(item => item.UserProfile)
            .Where(item => revieweeIds.Contains(item.UserId))
            .ToListAsync(cancellationToken);

        var revieweeLookup = reviewees.ToDictionary(
            item => item.UserId,
            item => (
                DisplayName: string.IsNullOrWhiteSpace(item.UserProfile?.DisplayName) ? item.Username : item.UserProfile!.DisplayName,
                AvatarUrl: item.UserProfile?.AvatarUrl));

        var now = _dateTimeProvider.UtcNow;
        var tasks = new List<ReviewTaskDto>();
        foreach (var session in sessions)
        {
            var revieweeId = session.CompanionId == currentUserId
                ? session.LearnerId
                : session.CompanionId;

            if (!revieweeId.HasValue)
            {
                continue;
            }

            var completedAt = session.DisbursedAt ?? session.UpdatedAt;
            var reviewWindowClosesAt = completedAt.AddHours(48);
            reviewLookup.TryGetValue(session.SessionId, out var existingReview);

            var reviewStatus = existingReview is not null
                ? "already_reviewed"
                : now <= reviewWindowClosesAt
                    ? "can_review"
                    : "window_closed";

            revieweeLookup.TryGetValue(revieweeId.Value, out var reviewee);
            tasks.Add(new ReviewTaskDto(
                session.SessionId,
                revieweeId.Value,
                reviewee.DisplayName ?? "Unknown",
                reviewee.AvatarUrl,
                session.Skill,
                session.PointCost,
                session.Description,
                reviewStatus,
                existingReview is null
                    ? null
                    : new ReviewDto(
                        existingReview.ReviewId,
                        existingReview.SessionId,
                        existingReview.ReviewerId,
                        existingReview.RevieweeId,
                        existingReview.Rating,
                        existingReview.Comment,
                        existingReview.CreatedAt),
                completedAt,
                reviewWindowClosesAt));
        }

        return tasks;
    }
}
