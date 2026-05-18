using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.MySpace.Commands.DeleteCompanionSpaceCard;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.MySpace;

public class DeleteCompanionSpaceCardCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCardExists_RemovesCard()
    {
        var userId = Guid.NewGuid();
        var cards = new List<CompanionSpaceCard>
        {
            new()
            {
                CompanionSpaceCardId = Guid.NewGuid(),
                UserId = userId,
                SkillId = Guid.NewGuid(),
                Skill = new Skill
                {
                    SkillId = Guid.NewGuid(),
                    Name = "Python",
                    Slug = "python",
                    BasePointCost = 100,
                    IsActive = true
                },
                Title = "Python"
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.CompanionSpaceCards).Returns(cards.BuildMockDbSet().Object);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var handler = new DeleteCompanionSpaceCardCommandHandler(contextMock.Object, currentUserServiceMock.Object);

        var result = await handler.Handle(new DeleteCompanionSpaceCardCommand(cards[0].CompanionSpaceCardId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cards.Should().BeEmpty();
    }
}
