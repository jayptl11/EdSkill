using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions.Commands.JoinSession;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class JoinSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionIsOffline_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = sessionId,
                CompanionId = userId,
                LearnerId = Guid.NewGuid(),
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Offline,
                Location = "District 1",
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddHours(2),
                Status = SessionStatus.Confirmed
            }
        };

        var presenceSegments = new List<SessionPresenceSegment>();
        var handler = CreateHandler(userId, sessions, presenceSegments, DateTime.UtcNow, out _);

        var result = await handler.Handle(new JoinSessionCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SESSION_NOT_ONLINE");
    }

    [Fact]
    public async Task Handle_WhenAlreadyJoined_ReturnsSuccessWithoutCreatingDuplicateSegment()
    {
        var now = new DateTime(2026, 5, 19, 9, 55, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = sessionId,
                CompanionId = userId,
                LearnerId = learnerId,
                Skill = "Python",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = now.AddMinutes(5),
                Status = SessionStatus.InProgress,
                JitsiRoomId = $"edskill-{sessionId:N}",
                ActualStartAt = now.AddMinutes(-2)
            }
        };
        var presenceSegments = new List<SessionPresenceSegment>
        {
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                JoinedAt = now.AddMinutes(-2)
            }
        };

        var handler = CreateHandler(userId, sessions, presenceSegments, now, out _);

        var result = await handler.Handle(new JoinSessionCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        presenceSegments.Should().HaveCount(1);
        sessions[0].Status.Should().Be(SessionStatus.InProgress);
    }

    [Fact]
    public async Task Handle_WhenLearnerJoinsBeforeCompanion_ReturnsHostNotReady()
    {
        var now = new DateTime(2026, 5, 19, 9, 55, 0, DateTimeKind.Utc);
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
                Skill = "Python",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = now.AddMinutes(5),
                Status = SessionStatus.Confirmed,
                JitsiRoomId = $"edskill-{sessionId:N}"
            }
        };
        var presenceSegments = new List<SessionPresenceSegment>();

        var handler = CreateHandler(learnerId, sessions, presenceSegments, now, out _);

        var result = await handler.Handle(new JoinSessionCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SESSION_HOST_NOT_READY");
        presenceSegments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCompanionJoinsFirst_ReturnsSuccessAndCreatesSegment()
    {
        var now = new DateTime(2026, 5, 19, 9, 55, 0, DateTimeKind.Utc);
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
                Skill = "Python",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = now.AddMinutes(5),
                Status = SessionStatus.Confirmed,
                JitsiRoomId = $"edskill-{sessionId:N}"
            }
        };
        var presenceSegments = new List<SessionPresenceSegment>();

        var handler = CreateHandler(companionId, sessions, presenceSegments, now, out _);

        var result = await handler.Handle(new JoinSessionCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        presenceSegments.Should().ContainSingle();
        presenceSegments[0].UserId.Should().Be(companionId);
        sessions[0].Status.Should().Be(SessionStatus.InProgress);
    }

    [Fact]
    public async Task Handle_WhenLearnerJoinsAfterCompanionIsReady_ReturnsSuccess()
    {
        var now = new DateTime(2026, 5, 19, 10, 0, 0, DateTimeKind.Utc);
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
                Skill = "Python",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = now,
                Status = SessionStatus.InProgress,
                JitsiRoomId = $"edskill-{sessionId:N}",
                ActualStartAt = now.AddMinutes(-2)
            }
        };
        var presenceSegments = new List<SessionPresenceSegment>
        {
            new()
            {
                SessionPresenceSegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = companionId,
                JoinedAt = now.AddMinutes(-2)
            }
        };

        var handler = CreateHandler(learnerId, sessions, presenceSegments, now, out _);

        var result = await handler.Handle(new JoinSessionCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        presenceSegments.Should().HaveCount(2);
        presenceSegments.Should().Contain(item => item.UserId == learnerId && item.LeftAt == null);
    }

    [Fact]
    public async Task Handle_WhenOutsideJoinWindow_ReturnsFailure()
    {
        var now = new DateTime(2026, 5, 19, 12, 31, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = sessionId,
                CompanionId = userId,
                LearnerId = learnerId,
                Skill = "Python",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = new DateTime(2026, 5, 19, 10, 0, 0, DateTimeKind.Utc),
                Status = SessionStatus.Confirmed,
                JitsiRoomId = $"edskill-{sessionId:N}"
            }
        };
        var presenceSegments = new List<SessionPresenceSegment>();

        var handler = CreateHandler(userId, sessions, presenceSegments, now, out _);

        var result = await handler.Handle(new JoinSessionCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SESSION_JOIN_WINDOW_CLOSED");
    }

    private static JoinSessionCommandHandler CreateHandler(
        Guid currentUserId,
        List<Session> sessions,
        List<SessionPresenceSegment> presenceSegments,
        DateTime now,
        out Mock<ISystemConfigService> systemConfigServiceMock)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.SessionPresenceSegments).Returns(presenceSegments.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(currentUserId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinEarlyMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinLateGraceMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(30);

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<SessionDto>(It.IsAny<Func<CancellationToken, Task<Result<SessionDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<SessionDto>>> operation, CancellationToken ct) => operation(ct));

        return new JoinSessionCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            transactionExecutorMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object);
    }
}
