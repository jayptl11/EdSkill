using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Reviews.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<ReviewDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITransactionExecutor _transactionExecutor;

    public CreateReviewCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ITransactionExecutor transactionExecutor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _transactionExecutor = transactionExecutor;
    }

    public async Task<Result<ReviewDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var reviewerId = _currentUserService.GetUserId();

        return await _transactionExecutor.ExecuteAsync<ReviewDto>(async ct =>
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(item => item.SessionId == request.SessionId, ct);
            if (session == null)
            {
                return Result<ReviewDto>.Failure("SESSION_NOT_FOUND", "Session was not found.");
            }

            if (session.Status != SessionStatus.Completed)
            {
                return Result<ReviewDto>.Failure("SESSION_INVALID_STATUS", "Hành động không hợp lệ với trạng thái hiện tại.");
            }

            if (session.CompanionId != reviewerId && session.LearnerId != reviewerId)
            {
                return Result<ReviewDto>.Failure("NOT_SESSION_PARTICIPANT", "Only session participants can review this session.");
            }

            var completedAt = session.DisbursedAt ?? session.UpdatedAt;
            if (_dateTimeProvider.UtcNow > completedAt.AddHours(48))
            {
                return Result<ReviewDto>.Failure("REVIEW_WINDOW_CLOSED", "The review window has closed.");
            }

            var exists = await _context.Reviews
                .AnyAsync(item => item.SessionId == request.SessionId && item.ReviewerId == reviewerId, ct);
            if (exists)
            {
                return Result<ReviewDto>.Failure("REVIEW_ALREADY_EXISTS", "You have already reviewed this session.");
            }

            var revieweeId = session.CompanionId == reviewerId
                ? session.LearnerId
                : session.CompanionId;
            if (!revieweeId.HasValue)
            {
                return Result<ReviewDto>.Failure("SESSION_INVALID_STATUS", "Hành động không hợp lệ với trạng thái hiện tại.");
            }

            var review = new Review
            {
                ReviewId = Guid.NewGuid(),
                SessionId = session.SessionId,
                ReviewerId = reviewerId,
                RevieweeId = revieweeId.Value,
                Rating = request.Rating,
                Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
                CreatedAt = _dateTimeProvider.UtcNow,
                UpdatedAt = _dateTimeProvider.UtcNow
            };

            await _context.Reviews.AddAsync(review, ct);

            return Result<ReviewDto>.Success(new ReviewDto(
                review.ReviewId,
                review.SessionId,
                review.ReviewerId,
                review.RevieweeId,
                review.Rating,
                review.Comment,
                review.CreatedAt));
        }, cancellationToken);
    }
}
