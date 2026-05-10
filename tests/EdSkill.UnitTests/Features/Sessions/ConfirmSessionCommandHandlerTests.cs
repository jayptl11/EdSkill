using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.Commands.ConfirmSession;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class ConfirmSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenOfflineSessionConfirmed_DoesNotGenerateJitsiRoom()
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
                Status = SessionStatus.Pending
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<SessionDto>(It.IsAny<Func<CancellationToken, Task<Result<SessionDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<SessionDto>>> operation, CancellationToken ct) => operation(ct));

        var handler = new ConfirmSessionCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            transactionExecutorMock.Object,
            dateTimeProviderMock.Object);

        var result = await handler.Handle(new ConfirmSessionCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sessions[0].Status.Should().Be(SessionStatus.Confirmed);
        sessions[0].JitsiRoomId.Should().BeNull();
    }
}
