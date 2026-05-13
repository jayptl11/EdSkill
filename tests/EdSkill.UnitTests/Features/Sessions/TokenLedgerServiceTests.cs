using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Services;
using EdSkill.Application.Common.System;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class TokenLedgerServiceTests
{
    [Fact]
    public async Task AwardSessionCompletionTokensAsync_WhenFormulaSessionAwardsTokens_RespectsCaps()
    {
        var learnerId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { UserId = learnerId, Username = "learner", TokenBalance = 0m },
            new() { UserId = companionId, Username = "companion", TokenBalance = 0m }
        };
        var tokenTransactions = new List<TokenTransaction>
        {
            new()
            {
                TokenTransactionId = Guid.NewGuid(),
                UserId = learnerId,
                User = users[0],
                Type = TokenTransactionType.SessionCompletionLearnerReward,
                Amount = 18m,
                BalanceBefore = 0m,
                BalanceAfter = 18m,
                CreatedAt = new DateTime(2026, 5, 12, 1, 0, 0, DateTimeKind.Utc)
            }
        };
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            LearnerId = learnerId,
            CompanionId = companionId,
            PricingModel = SessionPricingModel.FormulaV1,
            LearnerChargePoints = 100
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.TokenTransactions).Returns(tokenTransactions.BuildMockDbSet().Object);

        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.TokenDailyEarnLimit, It.IsAny<CancellationToken>())).ReturnsAsync(20);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.TokenWeeklyEarnLimit, It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 12, 2, 0, 0, DateTimeKind.Utc));

        var service = new TokenLedgerService(contextMock.Object, systemConfigServiceMock.Object, dateTimeProviderMock.Object);

        var result = await service.AwardSessionCompletionTokensAsync(session, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        users[0].TokenBalance.Should().Be(2m);
        users[1].TokenBalance.Should().Be(3m);
        tokenTransactions.Should().HaveCount(3);
        tokenTransactions.Last().Amount.Should().Be(3m);
    }

    [Fact]
    public async Task AwardSessionCompletionTokensAsync_WhenLegacySessionAwardsTokens_UsesConfiguredFixedValues()
    {
        var learnerId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { UserId = learnerId, Username = "learner", TokenBalance = 10m },
            new() { UserId = companionId, Username = "companion", TokenBalance = 20m }
        };
        var tokenTransactions = new List<TokenTransaction>();
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            LearnerId = learnerId,
            CompanionId = companionId,
            PricingModel = SessionPricingModel.LegacyManual,
            PointCost = 100
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.TokenTransactions).Returns(tokenTransactions.BuildMockDbSet().Object);

        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.TokenLearnerPerSession, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.TokenCompanionPerSession, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.TokenDailyEarnLimit, It.IsAny<CancellationToken>())).ReturnsAsync(100);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.TokenWeeklyEarnLimit, It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 12, 2, 0, 0, DateTimeKind.Utc));

        var service = new TokenLedgerService(contextMock.Object, systemConfigServiceMock.Object, dateTimeProviderMock.Object);

        var result = await service.AwardSessionCompletionTokensAsync(session, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        users[0].TokenBalance.Should().Be(15m);
        users[1].TokenBalance.Should().Be(23m);
        tokenTransactions.Select(x => x.Amount).Should().BeEquivalentTo(new[] { 5m, 3m });
    }

    [Fact]
    public async Task AwardSessionCompletionTokensAsync_WhenWeeklyCapIsReached_SkipsAdditionalAwards()
    {
        var learnerId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { UserId = learnerId, Username = "learner", TokenBalance = 40m },
            new() { UserId = companionId, Username = "companion", TokenBalance = 30m }
        };
        var tokenTransactions = new List<TokenTransaction>
        {
            new()
            {
                TokenTransactionId = Guid.NewGuid(),
                UserId = learnerId,
                User = users[0],
                Type = TokenTransactionType.SessionCompletionLearnerReward,
                Amount = 20m,
                BalanceBefore = 20m,
                BalanceAfter = 40m,
                CreatedAt = new DateTime(2026, 5, 11, 1, 0, 0, DateTimeKind.Utc)
            }
        };
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            LearnerId = learnerId,
            CompanionId = companionId,
            PricingModel = SessionPricingModel.FormulaV1,
            LearnerChargePoints = 100
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.TokenTransactions).Returns(tokenTransactions.BuildMockDbSet().Object);

        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.TokenDailyEarnLimit, It.IsAny<CancellationToken>())).ReturnsAsync(100);
        systemConfigServiceMock.Setup(x => x.GetIntValueAsync(SystemConfigKeys.TokenWeeklyEarnLimit, It.IsAny<CancellationToken>())).ReturnsAsync(20);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 12, 2, 0, 0, DateTimeKind.Utc));

        var service = new TokenLedgerService(contextMock.Object, systemConfigServiceMock.Object, dateTimeProviderMock.Object);

        var result = await service.AwardSessionCompletionTokensAsync(session, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        users[0].TokenBalance.Should().Be(40m);
        users[1].TokenBalance.Should().Be(33m);
        tokenTransactions.Should().HaveCount(2);
    }
}
