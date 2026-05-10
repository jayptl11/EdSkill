using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.Commands.BookSession;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class BookSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSelfBooking_ReturnsFailure()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var users = new List<User> { new() { UserId = userId, Roles = ["learner", "companion"] } };
        var sessions = new List<Session> { new() { SessionId = Guid.NewGuid(), CompanionId = userId, Status = SessionStatus.Available, PointCost = 100 } };

        var handler = CreateHandler(userId, users, sessions, _ => Result.Success());

        var result = await handler.Handle(new BookSessionCommand(sessions[0].SessionId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SELF_BOOKING");
    }

    [Fact]
    public async Task Handle_WhenInsufficientPoints_ReturnsFailure()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var sessions = new List<Session> { new() { SessionId = Guid.NewGuid(), CompanionId = Guid.NewGuid(), Status = SessionStatus.Available, PointCost = 100 } };

        var handler = CreateHandler(userId, users, sessions, _ => Result.Failure("INSUFFICIENT_POINTS", "Số điểm không đủ."));

        var result = await handler.Handle(new BookSessionCommand(sessions[0].SessionId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INSUFFICIENT_POINTS");
    }

    [Fact]
    public async Task Handle_WhenSessionAvailable_HoldsPointsAndSetsPending()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var sessions = new List<Session> { new() { SessionId = Guid.NewGuid(), CompanionId = Guid.NewGuid(), Status = SessionStatus.Available, PointCost = 100 } };

        var handler = CreateHandler(userId, users, sessions, session =>
        {
            session.Status = SessionStatus.Pending;
            return Result.Success();
        });

        var result = await handler.Handle(new BookSessionCommand(sessions[0].SessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sessions[0].LearnerId.Should().Be(userId);
        sessions[0].Status.Should().Be(SessionStatus.Pending);
    }

    private static BookSessionCommandHandler CreateHandler(
        Guid userId,
        List<User> users,
        List<Session> sessions,
        Func<Session, Result> holdResultFactory)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var wallet = new PointWallet { PointWalletId = Guid.NewGuid(), UserId = userId, Balance = 120 };
        var pointLedgerServiceMock = new Mock<IPointLedgerService>();
        pointLedgerServiceMock.Setup(x => x.GetOrCreateWalletAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        pointLedgerServiceMock
            .Setup(x => x.HoldPoints(wallet, It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string?>()))
            .Returns((PointWallet _, int __, Guid ___, string? ____) => holdResultFactory(sessions[0]));

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<SessionDto>(It.IsAny<Func<CancellationToken, Task<Result<SessionDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<SessionDto>>> operation, CancellationToken ct) => operation(ct));

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        return new BookSessionCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            pointLedgerServiceMock.Object,
            transactionExecutorMock.Object,
            dateTimeProviderMock.Object);
    }
}
