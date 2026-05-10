using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Skills.Commands.CreateSkill;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Skills;

public class CreateSkillCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly CreateSkillCommandHandler _handler;

    public CreateSkillCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _handler = new CreateSkillCommandHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSkillIsValid_CreatesSkill()
    {
        var skills = new List<Skill>();

        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateSkillCommand(" Speaking ", null, " Communication ", new[] { "Tiếng Anh" });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        skills.Should().ContainSingle();
        skills[0].Name.Should().Be("Speaking");
        skills[0].Slug.Should().Be("speaking");
        skills[0].Category.Should().Be("Communication");
        skills[0].Aliases.Should().BeEquivalentTo("Tiếng Anh");
    }

    [Fact]
    public async Task Handle_WhenAliasConflictsWithExistingSkill_ReturnsFailure()
    {
        var skills = new List<Skill>
        {
            new()
            {
                SkillId = Guid.NewGuid(),
                Name = "Speaking",
                Slug = "speaking",
                Aliases = new List<string> { "Presentation basics" },
                IsActive = true
            }
        };

        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var command = new CreateSkillCommand("Presentation", null, "Communication", new[] { "Speaking" });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SKILL_ALIAS_CONFLICT");
    }
}
