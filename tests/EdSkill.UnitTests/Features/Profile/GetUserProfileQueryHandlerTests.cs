using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Profile.Queries.GetUserProfile;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Profile;

public class GetUserProfileQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly GetUserProfileQueryHandler _handler;

    public GetUserProfileQueryHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _handler = new GetUserProfileQueryHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProfileIsPrivate_ReturnsFailure()
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { "companion" },
            UserSkills = new List<UserSkill>
            {
                new()
                {
                    UserSkillId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    SkillId = Guid.NewGuid(),
                    Skill = new Skill
                    {
                        SkillId = Guid.NewGuid(),
                        Name = "Speaking",
                        Slug = "speaking",
                        IsActive = true
                    },
                    Type = UserSkillType.Teach
                }
            },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                DisplayName = "Private User",
                IsPublic = false
            }
        };

        _contextMock.Setup(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetUserProfileQuery(user.UserId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PROFILE_PRIVATE");
    }

    [Fact]
    public async Task Handle_WhenProfileIsPublic_HidesPrivateFields()
    {
        var userId = Guid.NewGuid();
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
                    Skill = new Skill
                    {
                        SkillId = Guid.NewGuid(),
                        Name = "Speaking",
                        Slug = "speaking",
                        IsActive = true
                    },
                    Type = UserSkillType.Teach
                }
            },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                DisplayName = "Public User",
                AvatarUrl = "https://cdn.edskill.test/u/avatar.png",
                Bio = "I teach speaking",
                DateOfBirth = new DateTime(2000, 1, 2),
                Phone = "+84912345678",
                DegreeUrl = "https://cdn.edskill.test/degree/u/degree.pdf",
                IsPublic = true
            }
        };

        _contextMock.Setup(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetUserProfileQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SkillsToTeach.Should().BeEquivalentTo("Speaking");
        result.Value.SkillsToLearn.Should().BeEmpty();
        result.Value.TeachingSkills.Should().ContainSingle();
        result.Value.TeachingSkills.Single().Name.Should().Be("Speaking");
        result.Value.LearningSkills.Should().BeEmpty();
        result.Value.IsCompanionOnboardingComplete.Should().BeTrue();
        result.Value.MissingCompanionProfileFields.Should().BeEmpty();
        result.Value.DateOfBirth.Should().BeNull();
        result.Value.Phone.Should().BeNull();
        result.Value.DegreeUrl.Should().BeNull();
    }
}
