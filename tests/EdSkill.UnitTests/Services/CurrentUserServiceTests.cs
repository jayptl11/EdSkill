using EdSkill.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EdSkill.UnitTests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void TryGetUserId_WhenNameIdentifierClaimExists_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var result = service.TryGetUserId();

        result.Should().Be(userId);
    }

    [Fact]
    public void TryGetUserId_WhenSubClaimExists_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));

        var result = service.TryGetUserId();

        result.Should().Be(userId);
    }

    [Fact]
    public void GetUserId_WhenNoSupportedClaimExists_ThrowsUnauthorizedAccessException()
    {
        var service = CreateService(new Claim(ClaimTypes.Email, "user@edskill.test"));

        var action = () => service.GetUserId();

        action.Should().Throw<UnauthorizedAccessException>();
    }

    private static CurrentUserService CreateService(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
        };

        return new CurrentUserService(new HttpContextAccessor
        {
            HttpContext = httpContext
        });
    }
}
