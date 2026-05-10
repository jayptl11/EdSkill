using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Admin.Commands.UpdateSystemConfig;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Admin;

public class UpdateSystemConfigCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValidConfigValue_UpdatesConfig()
    {
        var now = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);
        var configs = new List<SystemConfig>
        {
            new() { Key = "point.signup_bonus", Value = "50", Description = "Signup bonus" },
            new() { Key = "session.late_cancel_companion_pct", Value = "80", Description = "Companion" },
            new() { Key = "session.late_cancel_platform_pct", Value = "20", Description = "Platform" }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.SystemConfigs).Returns(configs.BuildMockDbSet().Object);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var handler = new UpdateSystemConfigCommandHandler(contextMock.Object, currentUserServiceMock.Object, dateTimeProviderMock.Object);

        var result = await handler.Handle(new UpdateSystemConfigCommand("point.signup_bonus", "75"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("75");
        configs.Single(x => x.Key == "point.signup_bonus").UpdatedAt.Should().Be(now);
    }

    [Fact]
    public async Task Handle_WhenLateCancelPercentagesDoNotTotal100_ReturnsFailure()
    {
        var configs = new List<SystemConfig>
        {
            new() { Key = "session.late_cancel_companion_pct", Value = "80", Description = "Companion" },
            new() { Key = "session.late_cancel_platform_pct", Value = "20", Description = "Platform" }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.SystemConfigs).Returns(configs.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(Guid.NewGuid());

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        var handler = new UpdateSystemConfigCommandHandler(contextMock.Object, currentUserServiceMock.Object, dateTimeProviderMock.Object);

        var result = await handler.Handle(new UpdateSystemConfigCommand("session.late_cancel_companion_pct", "85"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SYSTEM_CONFIG_INVALID_VALUE");
    }
}
