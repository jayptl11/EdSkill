using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Skills.Commands.UpdateSkill;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Skills;

public class UpdateSkillCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly UpdateSkillCommandHandler _handler;

    public UpdateSkillCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _handler = new UpdateSkillCommandHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSkillExists_UpdatesAndHidesSkill()
    {
        var skillId = Guid.NewGuid();
        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "Speaking",
                Slug = "speaking",
                Category = "Communication",
                Aliases = new List<string> { "Tiếng Anh" },
                IsActive = true
            }
        };

        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateSkillCommand(
            skillId,
            true, "Presentation",
            false, null,
            true, "Communication",
            true, new[] { "Thuyết trình" },
            true, false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        skills[0].Name.Should().Be("Presentation");
        skills[0].Slug.Should().Be("speaking");
        skills[0].Aliases.Should().BeEquivalentTo("Thuyết trình");
        skills[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ReturnsFailure()
    {
        var skillId = Guid.NewGuid();
        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "Speaking",
                Slug = "speaking",
                IsActive = true
            },
            new()
            {
                SkillId = Guid.NewGuid(),
                Name = "Excel",
                Slug = "excel",
                IsActive = true
            }
        };

        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var command = new UpdateSkillCommand(
            skillId,
            false, null,
            true, "excel",
            false, null,
            false, null,
            false, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SKILL_SLUG_EXISTS");
    }
}
