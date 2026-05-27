using EdSkill.API.Hubs;
using EdSkill.API.Realtime;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class SignalRSessionRealtimePublisherTests
{
    [Fact]
    public async Task PublishSessionUpdatedAsync_WhenSessionChanges_UsesParticipantUserGroups()
    {
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var session = new SessionDto(
            Guid.NewGuid(),
            companionId,
            learnerId,
            "Python",
            "Realtime",
            SessionDeliveryMode.Online,
            null,
            60,
            100,
            SessionPricingModel.LegacyManual,
            Array.Empty<int>(),
            Array.Empty<SessionDurationPricingOptionDto>(),
            null,
            new SessionPricingPreviewDto(0, 0, 100, 100, 0, 0),
            null,
            DateTime.UtcNow.AddHours(1),
            SessionStatus.Pending,
            null,
            null,
            null,
            null,
            false,
            false,
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);

        var clientProxyMock = new Mock<IClientProxy>();
        clientProxyMock
            .Setup(x => x.SendCoreAsync(
                SessionRealtimeEventNames.SessionUpdated,
                It.Is<object?[]>(args => ReferenceEquals(args[0], session)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock
            .Setup(x => x.Groups(It.IsAny<IReadOnlyList<string>>()))
            .Returns(clientProxyMock.Object);

        var hubContextMock = new Mock<IHubContext<SessionRealtimeHub>>();
        hubContextMock.SetupGet(x => x.Clients).Returns(hubClientsMock.Object);

        var snapshotBuilderMock = new Mock<ISessionRealtimeSnapshotBuilder>();
        var loggerMock = new Mock<ILogger<SignalRSessionRealtimePublisher>>();

        var publisher = new SignalRSessionRealtimePublisher(
            hubContextMock.Object,
            snapshotBuilderMock.Object,
            loggerMock.Object);

        await publisher.PublishSessionUpdatedAsync(session, CancellationToken.None);

        hubClientsMock.Verify(
            x => x.Groups(It.Is<IReadOnlyList<string>>(groups =>
                groups.Count == 2
                && groups.Contains(SessionRealtimeGroupNames.User(companionId))
                && groups.Contains(SessionRealtimeGroupNames.User(learnerId)))),
            Times.Once);
        clientProxyMock.Verify(
            x => x.SendCoreAsync(
                SessionRealtimeEventNames.SessionUpdated,
                It.Is<object?[]>(args => ReferenceEquals(args[0], session)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishRoomStateUpdatedAsync_WhenSnapshotExists_UsesParticipantAndSessionGroups()
    {
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var snapshot = new SessionRoomStateSnapshot(
            new SessionRoomStateDto(
                sessionId,
                SessionStatus.InProgress,
                $"edskill-{sessionId:N}",
                true,
                false,
                1,
                DateTime.UtcNow.AddMinutes(-5),
                null,
                null,
                DateTime.UtcNow.AddMinutes(-15),
                DateTime.UtcNow.AddMinutes(75),
                DateTime.UtcNow),
            new[]
            {
                SessionRealtimeGroupNames.User(companionId),
                SessionRealtimeGroupNames.User(learnerId)
            });

        var clientProxyMock = new Mock<IClientProxy>();
        clientProxyMock
            .Setup(x => x.SendCoreAsync(
                SessionRealtimeEventNames.SessionRoomStateUpdated,
                It.Is<object?[]>(args => ReferenceEquals(args[0], snapshot.Payload)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock
            .Setup(x => x.Groups(It.IsAny<IReadOnlyList<string>>()))
            .Returns(clientProxyMock.Object);

        var hubContextMock = new Mock<IHubContext<SessionRealtimeHub>>();
        hubContextMock.SetupGet(x => x.Clients).Returns(hubClientsMock.Object);

        var snapshotBuilderMock = new Mock<ISessionRealtimeSnapshotBuilder>();
        snapshotBuilderMock
            .Setup(x => x.BuildRoomStateSnapshotAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var loggerMock = new Mock<ILogger<SignalRSessionRealtimePublisher>>();
        var publisher = new SignalRSessionRealtimePublisher(
            hubContextMock.Object,
            snapshotBuilderMock.Object,
            loggerMock.Object);

        await publisher.PublishRoomStateUpdatedAsync(sessionId, CancellationToken.None);

        hubClientsMock.Verify(
            x => x.Groups(It.Is<IReadOnlyList<string>>(groups =>
                groups.Count == 3
                && groups.Contains(SessionRealtimeGroupNames.User(companionId))
                && groups.Contains(SessionRealtimeGroupNames.User(learnerId))
                && groups.Contains(SessionRealtimeGroupNames.Session(sessionId)))),
            Times.Once);
        clientProxyMock.Verify(
            x => x.SendCoreAsync(
                SessionRealtimeEventNames.SessionRoomStateUpdated,
                It.Is<object?[]>(args => ReferenceEquals(args[0], snapshot.Payload)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
