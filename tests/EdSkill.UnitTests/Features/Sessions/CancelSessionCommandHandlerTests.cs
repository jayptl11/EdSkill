using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions.Commands.CancelSession;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class CancelSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenLearnerLateCancels_SplitsPointsEightyTwenty()
    {
        var learnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var companionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            LearnerId = learnerId,
            CompanionId = companionId,
            PointCost = 100,
            Status = SessionStatus.Confirmed,
            ScheduledAt = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc)
        };

        var sessions = new List<Session> { session };
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(learnerId);

        var learnerWallet = new PointWallet { UserId = learnerId, HeldBalance = 100 };
        var companionWallet = new PointWallet { UserId = companionId, Balance = 0 };
        var platformLedger = new SystemLedgerAccount { SystemLedgerAccountId = Guid.NewGuid(), Code = "platform_fee", Balance = 0 };

        var pointLedgerServiceMock = new Mock<IPointLedgerService>();
        pointLedgerServiceMock.Setup(x => x.GetOrCreateWalletAsync(learnerId, It.IsAny<CancellationToken>())).ReturnsAsync(learnerWallet);
        pointLedgerServiceMock.Setup(x => x.GetOrCreateWalletAsync(companionId, It.IsAny<CancellationToken>())).ReturnsAsync(companionWallet);
        pointLedgerServiceMock.Setup(x => x.CompleteSessionPayment(learnerWallet, 100, session.SessionId, It.IsAny<string?>())).Returns(Result.Success());
        pointLedgerServiceMock.Setup(x => x.CreditUser(companionWallet, PointTransactionType.SessionEarning, 80, session.SessionId, It.IsAny<string?>())).Returns(Result.Success());
        pointLedgerServiceMock.Setup(x => x.GetPlatformLedgerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(platformLedger);
        pointLedgerServiceMock.Setup(x => x.CreditPlatform(platformLedger, 20, session.SessionId, It.IsAny<string?>())).Returns(Result.Success());

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<SessionDto>(It.IsAny<Func<CancellationToken, Task<Result<SessionDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<SessionDto>>> operation, CancellationToken ct) => operation(ct));

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 10, 9, 0, 1, DateTimeKind.Utc));

        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionCancelDeadlineHours, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionLateCancelCompanionPct, It.IsAny<CancellationToken>())).ReturnsAsync(80);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionLateCancelPlatformPct, It.IsAny<CancellationToken>())).ReturnsAsync(20);

        var handler = new CancelSessionCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            pointLedgerServiceMock.Object,
            transactionExecutorMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object);

        var result = await handler.Handle(new CancelSessionCommand(session.SessionId, "late cancel"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Cancelled);
        pointLedgerServiceMock.Verify(x => x.CreditUser(companionWallet, PointTransactionType.SessionEarning, 80, session.SessionId, It.IsAny<string?>()), Times.Once);
        pointLedgerServiceMock.Verify(x => x.CreditPlatform(platformLedger, 20, session.SessionId, It.IsAny<string?>()), Times.Once);
    }
}
