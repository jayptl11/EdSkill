using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.MySpace.Commands.UpdateLearnerSpaceCard;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.MySpace;

public class UpdateLearnerSpaceCardCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCardExists_UpdatesLearnerCard()
    {
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var now = new DateTime(2026, 5, 18, 11, 0, 0, DateTimeKind.Utc);
        var skill = new Skill
        {
            SkillId = skillId,
            Name = "React",
            Slug = "react",
            IconKey = "code",
            BasePointCost = 120,
            IsActive = true
        };
        var card = new LearnerSpaceCard
        {
            LearnerSpaceCardId = Guid.NewGuid(),
            UserId = userId,
            SkillId = skillId,
            Skill = skill,
            Title = "Old title",
            Description = "Old description",
            TargetPoints = 150,
            DurationMinutes = 45,
            DeliveryModes = new List<SessionDeliveryMode> { SessionDeliveryMode.Online },
            Languages = new List<string> { "English" },
            IsPublished = false
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.LearnerSpaceCards).Returns(new[] { card }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        var handler = new UpdateLearnerSpaceCardCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object);

        var command = new UpdateLearnerSpaceCardCommand(
            card.LearnerSpaceCardId,
            false, null,
            true, "  React advanced  ",
            true, "  Improve fundamentals  ",
            true, 300,
            true, 90,
            true, new[] { SessionDeliveryMode.Online, SessionDeliveryMode.Offline },
            true, new[] { "English", "Tieng Viet" },
            true, "https://cdn.edskill.test/my-space/cover/u/react.png",
            true, true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        card.Title.Should().Be("React advanced");
        card.Description.Should().Be("Improve fundamentals");
        card.TargetPoints.Should().Be(300);
        card.DurationMinutes.Should().Be(90);
        card.IsPublished.Should().BeTrue();
        card.UpdatedAt.Should().Be(now);
    }
}
