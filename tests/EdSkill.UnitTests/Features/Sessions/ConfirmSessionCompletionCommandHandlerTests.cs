using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions.Commands.ConfirmSessionCompletion;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class ConfirmSessionCompletionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenBothSidesConfirmed_DisbursesOnceAndCompletesSession()
    {
        var learnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var companionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            LearnerId = learnerId,
            CompanionId = companionId,
            PointCost = 100,
            Status = SessionStatus.PendingReview,
            ActualDuration = 60,
            LearnerConfirmed = true
        };

        var sessions = new List<Session> { session };
        var profiles = new List<UserProfile>
        {
            new() { ProfileId = Guid.NewGuid(), UserId = learnerId, DisplayName = "Learner" },
            new() { ProfileId = Guid.NewGuid(), UserId = companionId, DisplayName = "Companion" }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.UserProfiles).Returns(profiles.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(companionId);

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
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));

        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionMinDurationMinutes, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.PointPlatformFeePct, It.IsAny<CancellationToken>())).ReturnsAsync(20);

        var handler = new ConfirmSessionCompletionCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            pointLedgerServiceMock.Object,
            transactionExecutorMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object);

        var result = await handler.Handle(new ConfirmSessionCompletionCommand(session.SessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Completed);
        session.CompanionConfirmed.Should().BeTrue();
        profiles.Should().OnlyContain(profile => profile.TotalSessions == 1);
        pointLedgerServiceMock.Verify(x => x.CreditUser(companionWallet, PointTransactionType.SessionEarning, 80, session.SessionId, It.IsAny<string?>()), Times.Once);
        pointLedgerServiceMock.Verify(x => x.CreditPlatform(platformLedger, 20, session.SessionId, It.IsAny<string?>()), Times.Once);
    }
}
