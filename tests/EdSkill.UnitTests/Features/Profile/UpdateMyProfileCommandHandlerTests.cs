using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Profile.Commands.UpdateMyProfile;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Profile;

public class UpdateMyProfileCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly UpdateMyProfileCommandHandler _handler;

    public UpdateMyProfileCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new UpdateMyProfileCommandHandler(_contextMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_UpdatesFieldsAndNormalizesValues()
    {
        var userId = Guid.NewGuid();
        var speakingSkill = new Skill
        {
            SkillId = Guid.NewGuid(),
            Name = "Speaking",
            Slug = "speaking",
            Category = "Communication",
            BasePointCost = 100,
            Aliases = new List<string> { "Tieng Anh" },
            IsActive = true
        };
        var aspNetSkill = new Skill
        {
            SkillId = Guid.NewGuid(),
            Name = "ASP.NET",
            Slug = "asp-net",
            Category = "Tech",
            BasePointCost = 120,
            IsActive = true
        };
        var reactSkill = new Skill
        {
            SkillId = Guid.NewGuid(),
            Name = "React",
            Slug = "react",
            Category = "Tech",
            BasePointCost = 90,
            IsActive = true
        };
        var skills = new List<Skill> { speakingSkill, aspNetSkill, reactSkill };
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
                    SkillId = Guid.NewGuid(),
                    Skill = new Skill { SkillId = Guid.NewGuid(), Name = "Old", Slug = "old", BasePointCost = 50, IsActive = true },
                    Type = UserSkillType.Teach
                }
            },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                DisplayName = "Old Name",
                SkillsToTeach = new List<string> { "Old" }
            }
        };

        _currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);
        _contextMock.Setup(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.UserSkills).Returns(user.UserSkills.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateMyProfileCommand(
            true, "  New Name  ",
            true, "  Hello world  ",
            true, new DateTime(2000, 1, 2),
            true, "  +84 912 345 678  ",
            false, null,
            true, new[] { "  https://cdn.edskill.test/degree/u/cert-1.pdf  ", "https://cdn.edskill.test/degree/u/cert-2.pdf" },
            true, new[] { " tieng anh ", "ASP.NET" },
            true, new[] { "React" },
            true, "  https://cdn.edskill.test/avatar/u/a.jpg  ",
            true, false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.UserProfile!.DisplayName.Should().Be("New Name");
        user.UserProfile.Bio.Should().Be("Hello world");
        user.UserProfile.DateOfBirth.Should().Be(new DateTime(2000, 1, 2));
        user.UserProfile.Phone.Should().Be("+84 912 345 678");
        user.UserProfile.DegreeUrl.Should().Be("https://cdn.edskill.test/degree/u/cert-1.pdf");
        user.UserProfile.CredentialUrls.Should().BeEquivalentTo(
            "https://cdn.edskill.test/degree/u/cert-1.pdf",
            "https://cdn.edskill.test/degree/u/cert-2.pdf");
        user.UserProfile.SkillsToTeach.Should().BeEquivalentTo("Speaking", "ASP.NET");
        user.UserProfile.SkillsToLearn.Should().BeEquivalentTo("React");
        user.UserProfile.AvatarUrl.Should().Be("https://cdn.edskill.test/avatar/u/a.jpg");
        user.UserProfile.IsPublic.Should().BeFalse();
        user.UserSkills.Should().HaveCount(3);
        user.UserSkills.Where(x => x.Type == UserSkillType.Teach)
            .Select(x => x.Skill.Name)
            .Should()
            .BeEquivalentTo("Speaking", "ASP.NET");
        user.UserSkills.Where(x => x.Type == UserSkillType.Learn)
            .Select(x => x.Skill.Name)
            .Should()
            .BeEquivalentTo("React");
    }

    [Fact]
    public async Task Handle_WhenProfileMissing_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId };

        _currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);
        _contextMock.Setup(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.UserSkills).Returns(new List<UserSkill>().BuildMockDbSet().Object);
        _contextMock.Setup(x => x.Skills).Returns(new List<Skill>().BuildMockDbSet().Object);

        var command = new UpdateMyProfileCommand(
            true, "Name",
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenSkillResolvesToInactive_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var inactiveSkill = new Skill
        {
            SkillId = Guid.NewGuid(),
            Name = "Canva",
            Slug = "canva",
            BasePointCost = 80,
            IsActive = false
        };
        var skills = new List<Skill> { inactiveSkill };
        var userSkills = new List<UserSkill>();
        var user = new User
        {
            UserId = userId,
            UserSkills = userSkills,
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                DisplayName = "Name"
            }
        };

        _currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);
        _contextMock.Setup(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.UserSkills).Returns(userSkills.BuildMockDbSet().Object);

        var command = new UpdateMyProfileCommand(
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            true, new[] { "Canva" },
            false, null,
            false, null,
            false, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SKILL_INACTIVE");
    }
}
