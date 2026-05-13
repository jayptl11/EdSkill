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

        var result = await handler.Handle(new BookSessionCommand(sessions[0].SessionId, 60), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SELF_BOOKING");
    }

    [Fact]
    public async Task Handle_WhenInsufficientPoints_ReturnsFailure()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var sessions = new List<Session> { new() { SessionId = Guid.NewGuid(), CompanionId = Guid.NewGuid(), Status = SessionStatus.Available, PointCost = 100 } };

        var handler = CreateHandler(userId, users, sessions, _ => Result.Failure("INSUFFICIENT_POINTS", "Insufficient."));

        var result = await handler.Handle(new BookSessionCommand(sessions[0].SessionId, 60), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INSUFFICIENT_POINTS");
    }

    [Fact]
    public async Task Handle_WhenFormulaSessionAvailable_ComputesSnapshotAndHoldsLearnerCharge()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var companionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var skillId = Guid.NewGuid();
        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                SkillId = skillId,
                Skill = "Speaking",
                PricingModel = SessionPricingModel.FormulaV1,
                DurationOptions = new List<int> { 45, 60 },
                DurationMinutes = 60,
                Status = SessionStatus.Available
            }
        };
        var skills = new List<Skill> { new() { SkillId = skillId, Name = "Speaking", Slug = "speaking", BasePointCost = 100, IsActive = true } };
        var profiles = new List<UserProfile> { new() { ProfileId = Guid.NewGuid(), UserId = companionId, DisplayName = "Companion", CredentialUrls = new List<string> { "https://cdn.edskill.test/cert.pdf" } } };

        var handler = CreateHandler(
            userId,
            users,
            sessions,
            session =>
            {
                session.Status = SessionStatus.Pending;
                return Result.Success();
            },
            skills,
            profiles,
            new FormulaSessionPricingSnapshot(45, 150, 188, 38, 100, 75, 75));

        var result = await handler.Handle(new BookSessionCommand(sessions[0].SessionId, 45), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sessions[0].LearnerId.Should().Be(userId);
        sessions[0].SelectedDurationMinutes.Should().Be(45);
        sessions[0].LearnerChargePoints.Should().Be(188);
        sessions[0].CompanionPayoutPoints.Should().Be(150);
        sessions[0].PlatformFeePoints.Should().Be(38);
        sessions[0].Status.Should().Be(SessionStatus.Pending);
    }

    [Fact]
    public async Task Handle_WhenFormulaSelectedDurationIsNotOffered_ReturnsFailure()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var companionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                SkillId = Guid.NewGuid(),
                Skill = "Speaking",
                PricingModel = SessionPricingModel.FormulaV1,
                DurationOptions = new List<int> { 45, 60 },
                DurationMinutes = 60,
                Status = SessionStatus.Available
            }
        };

        var handler = CreateHandler(userId, users, sessions, _ => Result.Success());

        var result = await handler.Handle(new BookSessionCommand(sessions[0].SessionId, 90), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_SELECTED_DURATION");
    }

    [Fact]
    public async Task Handle_WhenFormulaSnapshotChargeExceedsWallet_ReturnsInsufficientPoints()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var companionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var skillId = Guid.NewGuid();
        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                SkillId = skillId,
                Skill = "Speaking",
                PricingModel = SessionPricingModel.FormulaV1,
                DurationOptions = new List<int> { 45, 60 },
                DurationMinutes = 60,
                Status = SessionStatus.Available
            }
        };
        var skills = new List<Skill> { new() { SkillId = skillId, Name = "Speaking", Slug = "speaking", BasePointCost = 100, IsActive = true } };
        var profiles = new List<UserProfile> { new() { ProfileId = Guid.NewGuid(), UserId = companionId, DisplayName = "Companion", CredentialUrls = new List<string> { "https://cdn.edskill.test/cert.pdf" } } };

        var handler = CreateHandler(
            userId,
            users,
            sessions,
            _ => Result.Failure("INSUFFICIENT_POINTS", "Insufficient."),
            skills,
            profiles,
            new FormulaSessionPricingSnapshot(45, 150, 188, 38, 100, 75, 75));

        var result = await handler.Handle(new BookSessionCommand(sessions[0].SessionId, 45), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INSUFFICIENT_POINTS");
    }

    private static BookSessionCommandHandler CreateHandler(
        Guid userId,
        List<User> users,
        List<Session> sessions,
        Func<Session, Result> holdResultFactory,
        List<Skill>? skills = null,
        List<UserProfile>? profiles = null,
        FormulaSessionPricingSnapshot? pricingSnapshot = null)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Skills).Returns((skills ?? new List<Skill>()).BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.UserProfiles).Returns((profiles ?? new List<UserProfile>()).BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var wallet = new PointWallet { PointWalletId = Guid.NewGuid(), UserId = userId, Balance = 300 };
        var pointLedgerServiceMock = new Mock<IPointLedgerService>();
        pointLedgerServiceMock.Setup(x => x.GetOrCreateWalletAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        pointLedgerServiceMock
            .Setup(x => x.HoldPoints(wallet, It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string?>()))
            .Returns((PointWallet _, int __, Guid ___, string? ____) => holdResultFactory(sessions[0]));

        var sessionPricingServiceMock = new Mock<ISessionPricingService>();
        sessionPricingServiceMock.Setup(x => x.GetPlatformMarkupPctAsync(It.IsAny<CancellationToken>())).ReturnsAsync(25);
        sessionPricingServiceMock
            .Setup(x => x.BuildBookingSnapshot(It.IsAny<Skill>(), It.IsAny<int>(), It.IsAny<int>(), 25))
            .Returns(pricingSnapshot is null
                ? Result<FormulaSessionPricingSnapshot>.Failure("INVALID_SELECTED_DURATION", "Invalid.")
                : Result<FormulaSessionPricingSnapshot>.Success(pricingSnapshot));

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
            sessionPricingServiceMock.Object,
            transactionExecutorMock.Object,
            dateTimeProviderMock.Object);
    }
}
