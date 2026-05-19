using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Application.Features.Sessions.Queries.GetSessionRoomAccess;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class GetSessionRoomAccessQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenParticipantIsEligible_ReturnsRoomAccess()
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
        var users = new List<User>
        {
            new()
            {
                UserId = learnerId,
                Username = "learner01",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = learnerId,
                    DisplayName = "Learner One",
                    AvatarUrl = "https://cdn.edskill.test/learner.png"
                }
            }
        };

        var handler = CreateHandler(learnerId, sessions, users, now);

        var result = await handler.Handle(new GetSessionRoomAccessQuery(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new SessionRoomAccessDto(
            sessionId,
            $"edskill-{sessionId:N}",
            "meet.jit.si",
            "Learner One",
            "https://cdn.edskill.test/learner.png",
            "learner",
            SessionStatus.Confirmed,
            true,
            null,
            null,
            sessions[0].ScheduledAt,
            60,
            sessions[0].ScheduledAt.AddMinutes(-10),
            sessions[0].ScheduledAt.AddMinutes(90)));
    }

    [Fact]
    public async Task Handle_WhenUserIsNotParticipant_ReturnsForbidden()
    {
        var now = new DateTime(2026, 5, 19, 9, 55, 0, DateTimeKind.Utc);
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Python",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = now.AddMinutes(5),
                Status = SessionStatus.Confirmed,
                JitsiRoomId = "edskill-room"
            }
        };
        var users = new List<User>();

        var handler = CreateHandler(outsiderId, sessions, users, now);

        var result = await handler.Handle(new GetSessionRoomAccessQuery(sessions[0].SessionId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task Handle_WhenJoinWindowIsClosed_ReturnsSoftDeniedAccess()
    {
        var now = new DateTime(2026, 5, 19, 12, 31, 0, DateTimeKind.Utc);
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Python",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = new DateTime(2026, 5, 19, 10, 0, 0, DateTimeKind.Utc),
                Status = SessionStatus.Confirmed,
                JitsiRoomId = "edskill-room"
            }
        };
        var users = new List<User>
        {
            new() { UserId = learnerId, Username = "learner02" }
        };

        var handler = CreateHandler(learnerId, sessions, users, now);

        var result = await handler.Handle(new GetSessionRoomAccessQuery(sessions[0].SessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CanJoin.Should().BeFalse();
        result.Value.DenyCode.Should().Be("SESSION_JOIN_WINDOW_CLOSED");
    }

    private static GetSessionRoomAccessQueryHandler CreateHandler(
        Guid currentUserId,
        List<Session> sessions,
        List<User> users,
        DateTime now)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(currentUserId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinEarlyMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinLateGraceMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(30);

        return new GetSessionRoomAccessQueryHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object);
    }
}
