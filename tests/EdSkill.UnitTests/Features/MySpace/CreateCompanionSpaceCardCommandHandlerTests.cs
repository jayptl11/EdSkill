using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.MySpace.Commands.CreateCompanionSpaceCard;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.MySpace;

public class CreateCompanionSpaceCardCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSkillOwned_CreatesCompanionCard()
    {
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var now = new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);
        var skill = new Skill
        {
            SkillId = skillId,
            Name = "Python",
            Slug = "python",
            IconKey = "code",
            BasePointCost = 100,
            IsActive = true
        };
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
                    Skill = skill,
                    Type = UserSkillType.Teach
                }
            },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                DisplayName = "Companion"
            }
        };
        var cards = new List<CompanionSpaceCard>();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Users).Returns(new[] { user }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.CompanionSpaceCards).Returns(cards.BuildMockDbSet().Object);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        var handler = new CreateCompanionSpaceCardCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object);

        var command = new CreateCompanionSpaceCardCommand(
            skillId,
            "  Python basics  ",
            "  Intro course  ",
            250,
            60,
            new[] { SessionDeliveryMode.Online, SessionDeliveryMode.Offline },
            new[] { " English ", "Tieng Viet" },
            "https://cdn.edskill.test/my-space/cover/u/python.png",
            new[] { "https://cdn.edskill.test/my-space/credential/u/cert-1.pdf" },
            true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cards.Should().ContainSingle();
        cards.Single().Title.Should().Be("Python basics");
        cards.Single().Description.Should().Be("Intro course");
        cards.Single().Languages.Should().BeEquivalentTo("English", "Tieng Viet");
        cards.Single().DeliveryModes.Should().BeEquivalentTo(new[] { SessionDeliveryMode.Online, SessionDeliveryMode.Offline });
        cards.Single().CreatedAt.Should().Be(now);
        result.Value!.Skill.Name.Should().Be("Python");
    }
}
