using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Reviews.Queries.GetMyReviewDashboard;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Reviews;

public class GetMyReviewDashboardQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserHasReceivedReviewsAndTasks_ReturnsDashboard()
    {
        var currentUserId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var otherLearnerId = Guid.NewGuid();
        var now = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc);

        var canReviewSession = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = currentUserId,
            LearnerId = learnerId,
            Skill = "Python",
            Description = "Basics",
            PointCost = 250,
            Status = SessionStatus.Completed,
            DisbursedAt = now.AddHours(-12)
        };
        var alreadyReviewedSession = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = learnerId,
            LearnerId = currentUserId,
            Skill = "React",
            Description = "Hooks",
            PointCost = 300,
            Status = SessionStatus.Completed,
            DisbursedAt = now.AddHours(-24)
        };
        var closedWindowSession = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = currentUserId,
            LearnerId = otherLearnerId,
            Skill = "English",
            Description = "Speaking",
            PointCost = 200,
            Status = SessionStatus.Completed,
            DisbursedAt = now.AddHours(-72)
        };

        var receivedReview = new Review
        {
            ReviewId = Guid.NewGuid(),
            SessionId = canReviewSession.SessionId,
            ReviewerId = learnerId,
            RevieweeId = currentUserId,
            Rating = 5,
            Comment = "Great session",
            CreatedAt = now.AddHours(-6)
        };
        var existingReview = new Review
        {
            ReviewId = Guid.NewGuid(),
            SessionId = alreadyReviewedSession.SessionId,
            ReviewerId = currentUserId,
            RevieweeId = learnerId,
            Rating = 4,
            Comment = "Helpful learner",
            CreatedAt = now.AddHours(-2)
        };

        var users = new List<User>
        {
            new()
            {
                UserId = learnerId,
                Username = "learner1",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = learnerId,
                    DisplayName = "Learner One",
                    AvatarUrl = "https://cdn.edskill.test/avatar/learner-1.png"
                }
            },
            new()
            {
                UserId = otherLearnerId,
                Username = "learner2",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = otherLearnerId,
                    DisplayName = "Learner Two",
                    AvatarUrl = "https://cdn.edskill.test/avatar/learner-2.png"
                }
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Sessions).Returns(new[] { canReviewSession, alreadyReviewedSession, closedWindowSession }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.Reviews).Returns(new[] { receivedReview, existingReview }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.Users).Returns(users.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(currentUserId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        var handler = new GetMyReviewDashboardQueryHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object);

        var result = await handler.Handle(new GetMyReviewDashboardQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReceivedSummary.TotalReviews.Should().Be(1);
        result.Value.ReceivedSummary.AvgRating.Should().Be(5);
        result.Value.ReviewTasks.Should().HaveCount(3);
        result.Value.ReviewTasks.Single(task => task.SessionId == canReviewSession.SessionId).ReviewStatus.Should().Be("can_review");
        result.Value.ReviewTasks.Single(task => task.SessionId == alreadyReviewedSession.SessionId).ReviewStatus.Should().Be("already_reviewed");
        result.Value.ReviewTasks.Single(task => task.SessionId == closedWindowSession.SessionId).ReviewStatus.Should().Be("window_closed");
    }
}
