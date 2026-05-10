using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Skills.Commands.DeleteSkill;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Skills;

public class DeleteSkillCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly DeleteSkillCommandHandler _handler;

    public DeleteSkillCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _handler = new DeleteSkillCommandHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSkillExists_SetsIsDeletedToTrueAndIsActiveToFalse()
    {
        var skillId = Guid.NewGuid();
        var skill = new Skill
        {
            SkillId = skillId,
            Name = "Speaking",
            Slug = "speaking",
            IsActive = true,
            IsDeleted = false
        };
        var skills = new List<Skill> { skill };

        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteSkillCommand(skillId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        skill.IsActive.Should().BeFalse();
        skill.IsDeleted.Should().BeTrue();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSkillDoesNotExist_ReturnsNotFound()
    {
        var skills = new List<Skill>();

        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var command = new DeleteSkillCommand(Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SKILL_NOT_FOUND");
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
