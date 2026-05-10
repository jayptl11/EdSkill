using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Profile.Queries.GetUserProfile;
using EdSkill.Domain.Entities;
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
}
