using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Skills.Queries.GetAdminSkills;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Skills;

public class GetAdminSkillsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly GetAdminSkillsQueryHandler _handler;

    public GetAdminSkillsQueryHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _handler = new GetAdminSkillsQueryHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSkillsExist_ReturnsAdminDtosWithIconKey()
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
                BasePointCost = 100,
                Aliases = new List<string> { "Tieng Anh" },
                IsActive = true
            },
            new()
            {
                SkillId = Guid.NewGuid(),
                Name = "Canva",
                Slug = "canva",
                Category = "Design",
                IconKey = null,
                BasePointCost = 120,
                IsActive = true
            }
        };

        _contextMock.Setup(x => x.Skills).Returns(skills.BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetAdminSkillsQuery(null, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Single(x => x.Name == "Speaking").IconKey.Should().Be("languages");
        result.Value.Single(x => x.Name == "Canva").IconKey.Should().BeNull();
    }
}
