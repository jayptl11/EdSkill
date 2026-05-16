using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Services;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class AchievementAwardServiceTests
{
    [Fact]
    public async Task AwardForCompletedSessionAsync_WhenThresholdReached_AddsAchievement()
    {
        var learnerId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var definitionId = Guid.NewGuid();
        var completedAt = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            LearnerId = learnerId,
            CompanionId = companionId,
            Status = SessionStatus.Completed,
            ActualDuration = 60,
            DisbursedAt = completedAt
        };

        var definitions = new List<AchievementDefinition>
        {
            new()
            {
                AchievementDefinitionId = definitionId,
                Name = "First session",
                Description = "Description",
                Track = AchievementTrack.Companion,
                Metric = AchievementMetric.CompletedSessions,
                Threshold = 1,
                SortOrder = 1,
                IsActive = true,
                EffectiveFromUtc = completedAt.AddDays(-1)
            }
        };

        var sessions = new List<Session> { session };
        var userAchievements = new List<UserAchievement>();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.AchievementDefinitions).Returns(definitions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.UserAchievements).Returns(userAchievements.BuildMockDbSet().Object);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(completedAt);

        var service = new AchievementAwardService(contextMock.Object, dateTimeProviderMock.Object);

        await service.AwardForCompletedSessionAsync(session, CancellationToken.None);

        userAchievements.Should().ContainSingle(item =>
            item.UserId == companionId && item.AchievementDefinitionId == definitionId);
    }

    [Fact]
    public async Task AwardForCompletedSessionAsync_WhenDistinctLearnersRuleMet_AddsCompanionAchievement()
    {
        var companionId = Guid.NewGuid();
        var learnerOneId = Guid.NewGuid();
        var learnerTwoId = Guid.NewGuid();
        var definitionId = Guid.NewGuid();
        var completedAt = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);
        var currentSession = new Session
        {
            SessionId = Guid.NewGuid(),
            LearnerId = learnerTwoId,
            CompanionId = companionId,
            Status = SessionStatus.Completed,
            ActualDuration = 60,
            DisbursedAt = completedAt
        };

        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                LearnerId = learnerOneId,
                CompanionId = companionId,
                Status = SessionStatus.Completed,
                ActualDuration = 60,
                DisbursedAt = completedAt.AddDays(-1)
            },
            currentSession
        };

        var definitions = new List<AchievementDefinition>
        {
            new()
            {
                AchievementDefinitionId = definitionId,
                Name = "Two learners",
                Description = "Description",
                Track = AchievementTrack.Companion,
                Metric = AchievementMetric.DistinctCompletedLearners,
                Threshold = 2,
                SortOrder = 1,
                IsActive = true,
                EffectiveFromUtc = completedAt.AddDays(-7)
            }
        };

        var userAchievements = new List<UserAchievement>();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.AchievementDefinitions).Returns(definitions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.UserAchievements).Returns(userAchievements.BuildMockDbSet().Object);

        var service = new AchievementAwardService(contextMock.Object, Mock.Of<IDateTimeProvider>());

        await service.AwardForCompletedSessionAsync(currentSession, CancellationToken.None);

        userAchievements.Should().ContainSingle(item =>
            item.UserId == companionId && item.AchievementDefinitionId == definitionId);
    }
}
