using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Profile.Commands.EnableCompanion;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Profile;

public class EnableCompanionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserIsLearner_AddsCompanionRole()
    {
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            Roles = new List<string> { "learner" },
            UserSkills = new List<UserSkill>
            {
                new()
                {
                    UserSkillId = Guid.NewGuid(),
                    UserId = userId,
                    SkillId = skillId,
                    Skill = new Skill
                    {
                        SkillId = skillId,
                        Name = "Speaking",
                        Slug = "speaking",
                        IconKey = "languages",
                        IsActive = true
                    },
                    Type = UserSkillType.Teach
                }
            },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                DisplayName = "Teacher",
                IsPublic = false
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var handler = new EnableCompanionCommandHandler(contextMock.Object, currentUserServiceMock.Object);

        var result = await handler.Handle(new EnableCompanionCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Roles.Should().BeEquivalentTo("learner", "companion");
        result.Value!.Roles.Should().BeEquivalentTo("learner", "companion");
        result.Value.TeachingSkills.Should().ContainSingle();
        result.Value.IsCompanionOnboardingComplete.Should().BeFalse();
        result.Value.MissingCompanionProfileFields.Should().Contain("avatarUrl");
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyCompanion_DoesNotDuplicateRole()
    {
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            Roles = new List<string> { "learner", "companion" },
            UserSkills = new List<UserSkill>
            {
                new()
                {
                    UserSkillId = Guid.NewGuid(),
                    UserId = userId,
                    SkillId = skillId,
                    Skill = new Skill
                    {
                        SkillId = skillId,
                        Name = "Speaking",
                        Slug = "speaking",
                        IconKey = "languages",
                        IsActive = true
                    },
                    Type = UserSkillType.Teach
                }
            },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                DisplayName = "Teacher",
                IsPublic = true
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var handler = new EnableCompanionCommandHandler(contextMock.Object, currentUserServiceMock.Object);

        var result = await handler.Handle(new EnableCompanionCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Roles.Count(role => role == "companion").Should().Be(1);
        result.Value!.TeachingSkills.Should().ContainSingle();
        contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
