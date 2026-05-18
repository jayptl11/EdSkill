using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Achievements.Queries.GetMyAchievements;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Achievements;

public class GetMyAchievementsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserHasEarnedAndUpcomingAchievements_ReturnsProgress()
    {
        var userId = Guid.NewGuid();
        var earnedDefinition = new AchievementDefinition
        {
            AchievementDefinitionId = Guid.NewGuid(),
            Name = "First Session",
            Description = "Completed first session",
            Track = AchievementTrack.Learner,
            Metric = AchievementMetric.CompletedSessions,
            Threshold = 1,
            SortOrder = 1,
            IsActive = true,
            EffectiveFromUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var upcomingDefinition = new AchievementDefinition
        {
            AchievementDefinitionId = Guid.NewGuid(),
            Name = "Three Sessions",
            Description = "Completed three sessions",
            Track = AchievementTrack.Learner,
            Metric = AchievementMetric.CompletedSessions,
            Threshold = 3,
            SortOrder = 2,
            IsActive = true,
            EffectiveFromUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var earnedAchievement = new UserAchievement
        {
            UserAchievementId = Guid.NewGuid(),
            UserId = userId,
            AchievementDefinitionId = earnedDefinition.AchievementDefinitionId,
            AchievementDefinition = earnedDefinition,
            AwardedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
        };
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = Guid.NewGuid(),
                LearnerId = userId,
                Skill = "Python",
                Status = SessionStatus.Completed,
                ActualDuration = 60,
                DisbursedAt = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = Guid.NewGuid(),
                LearnerId = userId,
                Skill = "React",
                Status = SessionStatus.Completed,
                ActualDuration = 90,
                DisbursedAt = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.UserAchievements).Returns(new[] { earnedAchievement }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.AchievementDefinitions).Returns(new[] { earnedDefinition, upcomingDefinition }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var handler = new GetMyAchievementsQueryHandler(contextMock.Object, currentUserServiceMock.Object);

        var result = await handler.Handle(new GetMyAchievementsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Earned.Should().ContainSingle();
        result.Value.Upcoming.Should().ContainSingle();
        result.Value.Upcoming.Single().CurrentValue.Should().Be(2);
        result.Value.Upcoming.Single().RemainingValue.Should().Be(1);
        result.Value.Upcoming.Single().ProgressPercent.Should().BeApproximately(66.67, 0.01);
    }
}
