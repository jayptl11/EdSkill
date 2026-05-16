using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Admin.Commands.CreateAchievement;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Admin;

public class CreateAchievementCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestIsValid_PersistsAchievement()
    {
        var achievements = new List<AchievementDefinition>();
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.AchievementDefinitions).Returns(achievements.BuildMockDbSet().Object);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));

        var objectStorageServiceMock = new Mock<IObjectStorageService>();
        objectStorageServiceMock.Setup(x => x.IsPublicUrl("https://cdn.edskill.test/achievement/icon.png")).Returns(true);

        var handler = new CreateAchievementCommandHandler(contextMock.Object, dateTimeProviderMock.Object, objectStorageServiceMock.Object);

        var result = await handler.Handle(
            new CreateAchievementCommand(
                "First Teaching Session",
                "Teach the first completed session",
                "https://cdn.edskill.test/achievement/icon.png",
                "companion",
                "completed_sessions",
                1,
                10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        achievements.Should().ContainSingle();
        achievements[0].Name.Should().Be("First Teaching Session");
        achievements[0].Track.Should().Be(Domain.Enums.AchievementTrack.Companion);
        achievements[0].Metric.Should().Be(Domain.Enums.AchievementMetric.CompletedSessions);
    }
}
