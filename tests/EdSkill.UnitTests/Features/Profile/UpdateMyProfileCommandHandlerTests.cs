using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Profile.Commands.UpdateMyProfile;
using EdSkill.Domain.Entities;
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
        var user = new User
        {
            UserId = userId,
            Roles = new List<string> { "learner", "companion" },
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
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateMyProfileCommand(
            true, "  New Name  ",
            true, "  Hello world  ",
            true, "  FPT  ",
            true, "  SE  ",
            true, 4,
            true, new[] { "C#", " c# ", "ASP.NET" },
            true, new[] { "React" },
            true, "  https://cdn.edskill.test/avatar/u/a.jpg  ",
            true, false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.UserProfile!.DisplayName.Should().Be("New Name");
        user.UserProfile.Bio.Should().Be("Hello world");
        user.UserProfile.University.Should().Be("FPT");
        user.UserProfile.Faculty.Should().Be("SE");
        user.UserProfile.YearOfStudy.Should().Be(4);
        user.UserProfile.SkillsToTeach.Should().BeEquivalentTo("C#", "ASP.NET");
        user.UserProfile.SkillsToLearn.Should().BeEquivalentTo("React");
        user.UserProfile.AvatarUrl.Should().Be("https://cdn.edskill.test/avatar/u/a.jpg");
        user.UserProfile.IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenProfileMissing_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId };

        _currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);
        _contextMock.Setup(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);

        var command = new UpdateMyProfileCommand(
            true, "Name",
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
}
