using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Profile.Queries.GetMyProfile;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Profile;

public class GetMyProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenProfileExists_ReturnsStructuredOwnedSkills()
    {
        var userId = Guid.NewGuid();
        var teachSkillId = Guid.NewGuid();
        var learnSkillId = Guid.NewGuid();
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
                    SkillId = teachSkillId,
                    Skill = new Skill
                    {
                        SkillId = teachSkillId,
                        Name = "Speaking",
                        Slug = "speaking",
                        IconKey = "languages",
                        IsActive = true
                    },
                    Type = UserSkillType.Teach
                },
                new()
                {
                    UserSkillId = Guid.NewGuid(),
                    UserId = userId,
                    SkillId = learnSkillId,
                    Skill = new Skill
                    {
                        SkillId = learnSkillId,
                        Name = "React",
                        Slug = "react",
                        IconKey = "code",
                        IsActive = true
                    },
                    Type = UserSkillType.Learn
                }
            },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                DisplayName = "Companion",
                AvatarUrl = "https://cdn.edskill.test/u/avatar.png",
                Bio = "I teach speaking",
                DateOfBirth = new DateTime(2000, 1, 2),
                Phone = "+84912345678",
                DegreeUrl = "https://cdn.edskill.test/degree/u/degree.pdf",
                CredentialUrls = new List<string> { "https://cdn.edskill.test/degree/u/degree.pdf" },
                IsPublic = true
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var handler = new GetMyProfileQueryHandler(contextMock.Object, currentUserServiceMock.Object);

        var result = await handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SkillsToTeach.Should().BeEquivalentTo("Speaking");
        result.Value.SkillsToLearn.Should().BeEquivalentTo("React");
        result.Value.TeachingSkills.Should().ContainSingle();
        result.Value.TeachingSkills.Single().SkillId.Should().Be(teachSkillId);
        result.Value.TeachingSkills.Single().Name.Should().Be("Speaking");
        result.Value.TeachingSkills.Single().IconKey.Should().Be("languages");
        result.Value.LearningSkills.Should().ContainSingle();
        result.Value.LearningSkills.Single().SkillId.Should().Be(learnSkillId);
        result.Value.LearningSkills.Single().Name.Should().Be("React");
        result.Value.LearningSkills.Single().IconKey.Should().Be("code");
    }
}
