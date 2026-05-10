using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
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
                IsActive = true
            }
        };

        var result = await CreateHandler(userId, users, [], skills).Handle(
            new CreateSessionOfferCommand(skillId, "Desc", SessionDeliveryMode.Online, null, 60, 100, DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("COMPANION_PROFILE_INCOMPLETE");
    }

    [Fact]
    public async Task Handle_WhenCompanionProfileComplete_CreatesAvailableSession()
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
                IsActive = true
            }
        };

        var result = await CreateHandler(userId, users, sessions, skills).Handle(
            new CreateSessionOfferCommand(skillId, "Desc", SessionDeliveryMode.Online, null, 60, 100, DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sessions.Should().HaveCount(1);
        sessions[0].Status.Should().Be(SessionStatus.Available);
        sessions[0].CompanionId.Should().Be(userId);
        sessions[0].Skill.Should().Be("Speaking");
        sessions[0].DeliveryMode.Should().Be(SessionDeliveryMode.Online);
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
            .Setup(x => x.GetIntValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<SessionDto>(It.IsAny<Func<CancellationToken, Task<Result<SessionDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<SessionDto>>> operation, CancellationToken ct) => operation(ct));

        return new CreateSessionOfferCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            systemConfigServiceMock.Object,
            transactionExecutorMock.Object);
    }
}
