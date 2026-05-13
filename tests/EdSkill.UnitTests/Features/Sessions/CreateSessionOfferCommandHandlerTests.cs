using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class CreateSessionOfferCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCompanionProfileIncomplete_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var users = new List<User>
        {
            new()
            {
                UserId = userId,
                Roles = new List<string> { "learner", "companion" },
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = userId,
                    DisplayName = "Teacher",
                    Bio = "I teach speaking",
                    DateOfBirth = new DateTime(2000, 1, 2),
                    Phone = "+84912345678",
                    SkillsToTeach = new List<string> { "Speaking" },
                    IsPublic = false
                }
            }
        };
        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "Speaking",
                Slug = "speaking",
                BasePointCost = 100,
                IsActive = true
            }
        };

        var result = await CreateHandler(userId, users, [], skills).Handle(
            new CreateSessionOfferCommand(skillId, "Desc", SessionDeliveryMode.Online, null, new[] { 45, 60 }, DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("COMPANION_PROFILE_INCOMPLETE");
    }

    [Fact]
    public async Task Handle_WhenCompanionProfileComplete_CreatesFormulaSession()
    {
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var sessions = new List<Session>();
        var users = new List<User>
        {
            new()
            {
                UserId = userId,
                Roles = new List<string> { "learner", "companion" },
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = userId,
                    DisplayName = "Teacher",
                    AvatarUrl = "https://cdn.edskill.test/u/avatar.png",
                    Bio = "I teach speaking",
                    DateOfBirth = new DateTime(2000, 1, 2),
                    Phone = "+84912345678",
                    SkillsToTeach = new List<string> { "Speaking" },
                    CredentialUrls = new List<string> { "https://cdn.edskill.test/degree/u/cert.pdf" },
                    IsPublic = true
                }
            }
        };
        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "Speaking",
                Slug = "speaking",
                BasePointCost = 100,
                IsActive = true
            }
        };

        var result = await CreateHandler(userId, users, sessions, skills).Handle(
            new CreateSessionOfferCommand(skillId, "Desc", SessionDeliveryMode.Online, null, new[] { 45, 60 }, DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sessions.Should().HaveCount(1);
        sessions[0].Status.Should().Be(SessionStatus.Available);
        sessions[0].PricingModel.Should().Be(SessionPricingModel.FormulaV1);
        sessions[0].DurationOptions.Should().BeEquivalentTo(new[] { 45, 60 });
        sessions[0].DurationMinutes.Should().Be(60);
        result.Value!.PricingPreview.MinLearnerChargePoints.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WhenDurationOptionsOverlapExistingReservedSlot_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddDays(1);
        var users = new List<User>
        {
            new()
            {
                UserId = userId,
                Roles = new List<string> { "learner", "companion" },
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = userId,
                    DisplayName = "Teacher",
                    AvatarUrl = "https://cdn.edskill.test/u/avatar.png",
                    Bio = "I teach speaking",
                    DateOfBirth = new DateTime(2000, 1, 2),
                    Phone = "+84912345678",
                    SkillsToTeach = new List<string> { "Speaking" },
                    CredentialUrls = new List<string> { "https://cdn.edskill.test/degree/u/cert.pdf" },
                    IsPublic = true
                }
            }
        };
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = userId,
                DurationMinutes = 120,
                ScheduledAt = scheduledAt.AddMinutes(30),
                Status = SessionStatus.Available
            }
        };
        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "Speaking",
                Slug = "speaking",
                BasePointCost = 100,
                IsActive = true
            }
        };

        var result = await CreateHandler(userId, users, sessions, skills).Handle(
            new CreateSessionOfferCommand(skillId, "Desc", SessionDeliveryMode.Online, null, new[] { 45, 120 }, scheduledAt),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SESSION_TIME_CONFLICT");
    }

    private static CreateSessionOfferCommandHandler CreateHandler(
        Guid userId,
        List<User> users,
        List<Session> sessions,
        List<Skill> skills)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        systemConfigServiceMock
            .Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionMaxPerDayPerCompanion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);
        systemConfigServiceMock
            .Setup(x => x.GetIntValueAsync(SystemConfigKeys.SessionBufferMinutes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var sessionPricingServiceMock = new Mock<ISessionPricingService>();
        sessionPricingServiceMock.Setup(x => x.GetPlatformMarkupPctAsync(It.IsAny<CancellationToken>())).ReturnsAsync(25);
        sessionPricingServiceMock
            .Setup(x => x.BuildOfferPreview(It.IsAny<Skill>(), It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), 25))
            .Returns(Result<SessionPricingPreview>.Success(new SessionPricingPreview(135, 175, 169, 219, 34, 44)));

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<SessionDto>(It.IsAny<Func<CancellationToken, Task<Result<SessionDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<SessionDto>>> operation, CancellationToken ct) => operation(ct));

        return new CreateSessionOfferCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object,
            sessionPricingServiceMock.Object,
            transactionExecutorMock.Object);
    }
}
