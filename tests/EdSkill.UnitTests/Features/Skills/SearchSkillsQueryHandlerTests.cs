using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Skills.Queries.SearchSkills;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Skills;

public class SearchSkillsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly SearchSkillsQueryHandler _handler;

    public SearchSkillsQueryHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _handler = new SearchSkillsQueryHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSearchingByAliasAndCategory_ReturnsActiveMatchesOnly()
    {
        var skills = new List<Skill>
        {
            new()
            {
                SkillId = Guid.NewGuid(),
                Name = "Speaking",
                Slug = "speaking",
                Category = "Communication",
                IconKey = "languages",
                Aliases = new List<string> { "Tieng Anh" },
                IsActive = true
            },
            new()
            {
                SkillId = Guid.NewGuid(),
                Name = "Excel",
                Slug = "excel",
                Category = "Productivity",
                IconKey = null,
                IsActive = true
            },
            new()
            {
                SkillId = Guid.NewGuid(),
                Name = "Canva",
                Slug = "canva",
                Category = "Design",
                IsActive = false
            }
        };

        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var result = await _handler.Handle(new SearchSkillsQuery("tieng anh", "communication", 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value!.Single().Name.Should().Be("Speaking");
        result.Value.Single().IconKey.Should().Be("languages");
    }

    [Fact]
    public async Task Handle_WhenSkillHasNoIcon_ReturnsNullIconKey()
    {
        var skills = new List<Skill>
        {
            new()
            {
                SkillId = Guid.NewGuid(),
                Name = "Excel",
                Slug = "excel",
                Category = "Productivity",
                IconKey = null,
                IsActive = true
            }
        };

        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var result = await _handler.Handle(new SearchSkillsQuery("excel", null, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value!.Single().IconKey.Should().BeNull();
    }
}
