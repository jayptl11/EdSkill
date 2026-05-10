using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Reviews.Commands.CreateReview;
using EdSkill.Application.Features.Reviews.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Reviews;

public class CreateReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenReviewIsValid_CreatesReview()
    {
        var learnerId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddDays(-1),
                Status = SessionStatus.Completed,
                DisbursedAt = DateTime.UtcNow.AddHours(-2)
            }
        };
        var reviews = new List<Review>();

        var result = await CreateHandler(learnerId, sessions, reviews, DateTime.UtcNow).Handle(
            new CreateReviewCommand(sessionId, 5, "Great"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reviews.Should().ContainSingle();
        reviews[0].RevieweeId.Should().Be(companionId);
        reviews[0].Rating.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenReviewWindowClosed_ReturnsFailure()
    {
        var learnerId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = now.AddDays(-3),
                Status = SessionStatus.Completed,
                DisbursedAt = now.AddHours(-49)
            }
        };

        var result = await CreateHandler(learnerId, sessions, [], now).Handle(
            new CreateReviewCommand(sessionId, 4, "Late"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("REVIEW_WINDOW_CLOSED");
    }

    private static CreateReviewCommandHandler CreateHandler(
        Guid currentUserId,
        List<Session> sessions,
        List<Review> reviews,
        DateTime now)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(reviews.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(currentUserId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<ReviewDto>(It.IsAny<Func<CancellationToken, Task<Result<ReviewDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<ReviewDto>>> operation, CancellationToken ct) => operation(ct));

        return new CreateReviewCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            transactionExecutorMock.Object);
    }
}
