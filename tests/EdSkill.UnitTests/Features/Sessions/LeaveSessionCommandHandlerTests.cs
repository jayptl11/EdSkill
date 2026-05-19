using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions.Commands.LeaveSession;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class LeaveSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenOtherParticipantStillPresent_KeepsSessionInProgress()
    {
        var now = new DateTime(2026, 5, 19, 10, 20, 0, DateTimeKind.Utc);
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Excel",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = now.AddMinutes(-20),
                Status = SessionStatus.InProgress,
                JitsiRoomId = $"edskill-{sessionId:N}",
                ActualStartAt = now.AddMinutes(-15)
            }
        };
        var presenceSegments = new List<SessionPresenceSegment>
        {
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = companionId,
                JoinedAt = now.AddMinutes(-15)
            },
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = learnerId,
                JoinedAt = now.AddMinutes(-14)
            }
        };

        var handler = CreateHandler(companionId, sessions, presenceSegments, now);

        var result = await handler.Handle(new LeaveSessionCommand(sessionId, 999), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sessions[0].Status.Should().Be(SessionStatus.InProgress);
        sessions[0].ActualEndAt.Should().BeNull();
        sessions[0].ActualDuration.Should().BeNull();
        presenceSegments.Single(item => item.UserId == companionId).LeftAt.Should().Be(now);
    }

    [Fact]
    public async Task Handle_WhenLastParticipantLeavesAndDurationIsEnough_SetsPendingReview()
    {
        var now = new DateTime(2026, 5, 19, 10, 40, 0, DateTimeKind.Utc);
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Excel",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = now.AddMinutes(-40),
                Status = SessionStatus.InProgress,
                JitsiRoomId = $"edskill-{sessionId:N}",
                ActualStartAt = now.AddMinutes(-35)
            }
        };
        var presenceSegments = new List<SessionPresenceSegment>
        {
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = learnerId,
                JoinedAt = now.AddMinutes(-35),
                LeftAt = now.AddMinutes(-5)
            },
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = companionId,
                JoinedAt = now.AddMinutes(-30)
            }
        };

        var handler = CreateHandler(companionId, sessions, presenceSegments, now);

        var result = await handler.Handle(new LeaveSessionCommand(sessionId, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sessions[0].Status.Should().Be(SessionStatus.PendingReview);
        sessions[0].ActualEndAt.Should().Be(now);
        sessions[0].ActualDuration.Should().Be(25);
    }

    [Fact]
    public async Task Handle_WhenReconnectCreatesMultipleSegments_CalculatesSharedOverlap()
    {
        var now = new DateTime(2026, 5, 19, 11, 0, 0, DateTimeKind.Utc);
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Excel",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = now.AddMinutes(-50),
                Status = SessionStatus.InProgress,
                JitsiRoomId = $"edskill-{sessionId:N}",
                ActualStartAt = now.AddMinutes(-45)
            }
        };
        var presenceSegments = new List<SessionPresenceSegment>
        {
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = learnerId,
                JoinedAt = now.AddMinutes(-45),
                LeftAt = now.AddMinutes(-30)
            },
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = learnerId,
                JoinedAt = now.AddMinutes(-20),
                LeftAt = now.AddMinutes(-5)
            },
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = companionId,
                JoinedAt = now.AddMinutes(-40),
                LeftAt = now.AddMinutes(-25)
            },
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = companionId,
                JoinedAt = now.AddMinutes(-15)
            }
        };

        var handler = CreateHandler(companionId, sessions, presenceSegments, now, 25);

        var result = await handler.Handle(new LeaveSessionCommand(sessionId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sessions[0].Status.Should().Be(SessionStatus.Disputed);
        sessions[0].ActualDuration.Should().Be(20);
    }

    private static LeaveSessionCommandHandler CreateHandler(
        Guid currentUserId,
        List<Session> sessions,
        List<SessionPresenceSegment> presenceSegments,
        DateTime now,
        int minDuration = 15)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.SessionPresenceSegments).Returns(presenceSegments.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(currentUserId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionMinDurationMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(minDuration);

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<SessionDto>(It.IsAny<Func<CancellationToken, Task<Result<SessionDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<SessionDto>>> operation, CancellationToken ct) => operation(ct));

        return new LeaveSessionCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            transactionExecutorMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object);
    }
}
