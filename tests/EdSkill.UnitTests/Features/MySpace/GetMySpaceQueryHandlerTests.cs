using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.MySpace.Queries.GetMySpace;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.MySpace;

public class GetMySpaceQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserHasOpenedAndBookedSessions_ReturnsSplitSessionLists()
    {
        var now = new DateTime(2026, 5, 23, 8, 55, 0, DateTimeKind.Utc);
        var currentUserId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var openedAvailableSession = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = currentUserId,
            SkillId = skillId,
            Skill = "Python",
            Description = "Open basics session",
            DurationMinutes = 60,
            PointCost = 250,
            ScheduledAt = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc),
            Status = SessionStatus.Available,
            DeliveryMode = SessionDeliveryMode.Online
        };

        var openedBookedSession = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = currentUserId,
            LearnerId = learnerId,
            SkillId = skillId,
            Skill = "Python",
            Description = "Booked coaching session",
            DurationMinutes = 90,
            PointCost = 400,
            ScheduledAt = new DateTime(2026, 5, 23, 9, 0, 0, DateTimeKind.Utc),
            Status = SessionStatus.Pending,
            DeliveryMode = SessionDeliveryMode.Online
        };

        var learnerBookedSession = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = companionId,
            LearnerId = currentUserId,
            SkillId = skillId,
            Skill = "Python",
            Description = "Learning session",
            DurationMinutes = 45,
            PointCost = 200,
            ScheduledAt = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc),
            Status = SessionStatus.Confirmed,
            DeliveryMode = SessionDeliveryMode.Online,
            JitsiRoomId = "edskill-room"
        };

        var users = new List<User>
        {
            new()
            {
                UserId = currentUserId,
                Username = "current-user",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = currentUserId,
                    DisplayName = "Current User",
                    AvatarUrl = "https://cdn.edskill.test/avatar/current-user.png"
                }
            },
            new()
            {
                UserId = learnerId,
                Username = "learner-user",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = learnerId,
                    DisplayName = "Learner User",
                    AvatarUrl = "https://cdn.edskill.test/avatar/learner-user.png"
                }
            },
            new()
            {
                UserId = companionId,
                Username = "companion-user",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = companionId,
                    DisplayName = "Companion User",
                    AvatarUrl = "https://cdn.edskill.test/avatar/companion-user.png"
                }
            }
        };

        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "Python",
                IconKey = "code"
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(new[] { openedAvailableSession, openedBookedSession, learnerBookedSession }.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.SessionPresenceSegments).Returns(Array.Empty<SessionPresenceSegment>().BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(currentUserId);

        var sessionPricingServiceMock = new Mock<ISessionPricingService>();
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);
        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinEarlyMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinLateGraceMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(30);

        var handler = new GetMySpaceQueryHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            sessionPricingServiceMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object);

        var result = await handler.Handle(new GetMySpaceQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CompanionSessions.Should().HaveCount(2);
        result.Value.LearnerSessions.Should().HaveCount(1);

        var bookedCompanionSession = result.Value.CompanionSessions.Single(item => item.Session.SessionId == openedBookedSession.SessionId);
        bookedCompanionSession.Skill!.IconKey.Should().Be("code");
        bookedCompanionSession.Companion.DisplayName.Should().Be("Current User");
        bookedCompanionSession.Learner!.DisplayName.Should().Be("Learner User");

        var availableCompanionSession = result.Value.CompanionSessions.Single(item => item.Session.SessionId == openedAvailableSession.SessionId);
        availableCompanionSession.Learner.Should().BeNull();
        availableCompanionSession.RoomAccess!.CanOpenRoomPage.Should().BeFalse();
        availableCompanionSession.RoomAccess.DenyCode.Should().Be("SESSION_ROOM_NOT_READY");

        var learnerSession = result.Value.LearnerSessions.Single();
        learnerSession.Session.SessionId.Should().Be(learnerBookedSession.SessionId);
        learnerSession.Companion.DisplayName.Should().Be("Companion User");
        learnerSession.Learner!.DisplayName.Should().Be("Current User");
        learnerSession.RoomAccess!.CanOpenRoomPage.Should().BeFalse();
        learnerSession.RoomAccess.CanJoinNow.Should().BeFalse();
        learnerSession.RoomAccess.DenyCode.Should().Be("SESSION_JOIN_WINDOW_CLOSED");

        sessionPricingServiceMock.Verify(
            x => x.GetPlatformMarkupPctAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoSessions_ReturnsEmptyLists()
    {
        var currentUserId = Guid.NewGuid();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(Array.Empty<Session>().BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(currentUserId);

        var sessionPricingServiceMock = new Mock<ISessionPricingService>();

        var handler = new GetMySpaceQueryHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            sessionPricingServiceMock.Object,
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ISystemConfigService>());

        var result = await handler.Handle(new GetMySpaceQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CompanionSessions.Should().BeEmpty();
        result.Value.LearnerSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSessionsArePastTheirEndTime_HidesThemFromMySpace()
    {
        var now = new DateTime(2026, 5, 23, 22, 6, 0, DateTimeKind.Utc);
        var currentUserId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var pastLearnerSession = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = companionId,
            LearnerId = currentUserId,
            SkillId = skillId,
            Skill = "JavaScript",
            DeliveryMode = SessionDeliveryMode.Online,
            DurationMinutes = 120,
            PointCost = 500,
            ScheduledAt = new DateTime(2026, 5, 23, 19, 35, 0, DateTimeKind.Utc),
            Status = SessionStatus.InProgress,
            JitsiRoomId = "edskill-room"
        };

        var pastCompanionSession = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = currentUserId,
            LearnerId = companionId,
            SkillId = skillId,
            Skill = "JavaScript",
            DeliveryMode = SessionDeliveryMode.Online,
            DurationMinutes = 60,
            PointCost = 300,
            ScheduledAt = new DateTime(2026, 5, 23, 20, 0, 0, DateTimeKind.Utc),
            Status = SessionStatus.Confirmed,
            JitsiRoomId = "edskill-room-2"
        };

        var users = new List<User>
        {
            new()
            {
                UserId = currentUserId,
                Username = "current-user",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = currentUserId,
                    DisplayName = "Current User"
                }
            },
            new()
            {
                UserId = companionId,
                Username = "companion-user",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = companionId,
                    DisplayName = "Companion User"
                }
            }
        };

        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "JavaScript",
                IconKey = "code"
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(new[] { pastLearnerSession, pastCompanionSession }.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.SessionPresenceSegments).Returns(Array.Empty<SessionPresenceSegment>().BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(currentUserId);

        var sessionPricingServiceMock = new Mock<ISessionPricingService>();
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);
        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinEarlyMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinLateGraceMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(30);

        var handler = new GetMySpaceQueryHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            sessionPricingServiceMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object);

        var result = await handler.Handle(new GetMySpaceQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CompanionSessions.Should().BeEmpty();
        result.Value.LearnerSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenLearnerIsWithinJoinWindowButHostHasNotJoined_ReturnsWaitingStateRoomAccess()
    {
        var now = new DateTime(2026, 5, 23, 18, 55, 0, DateTimeKind.Utc);
        var currentUserId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var learnerBookedSession = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = companionId,
            LearnerId = currentUserId,
            SkillId = skillId,
            Skill = "JavaScript",
            DeliveryMode = SessionDeliveryMode.Online,
            DurationMinutes = 120,
            PointCost = 500,
            ScheduledAt = new DateTime(2026, 5, 23, 19, 0, 0, DateTimeKind.Utc),
            Status = SessionStatus.Confirmed,
            JitsiRoomId = "edskill-room"
        };

        var users = new List<User>
        {
            new()
            {
                UserId = currentUserId,
                Username = "current-user",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = currentUserId,
                    DisplayName = "Current User"
                }
            },
            new()
            {
                UserId = companionId,
                Username = "companion-user",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = companionId,
                    DisplayName = "Companion User"
                }
            }
        };

        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "JavaScript",
                IconKey = "code"
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(new[] { learnerBookedSession }.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.SessionPresenceSegments).Returns(Array.Empty<SessionPresenceSegment>().BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(currentUserId);

        var sessionPricingServiceMock = new Mock<ISessionPricingService>();
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);
        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinEarlyMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionJoinLateGraceMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(30);

        var handler = new GetMySpaceQueryHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            sessionPricingServiceMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object);

        var result = await handler.Handle(new GetMySpaceQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var learnerSession = result.Value!.LearnerSessions.Single();
        learnerSession.RoomAccess.Should().NotBeNull();
        learnerSession.RoomAccess!.CanOpenRoomPage.Should().BeTrue();
        learnerSession.RoomAccess.CanJoinNow.Should().BeFalse();
        learnerSession.RoomAccess.DenyCode.Should().Be("SESSION_HOST_NOT_READY");
    }
}
