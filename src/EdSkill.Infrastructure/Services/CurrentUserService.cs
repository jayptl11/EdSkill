using EdSkill.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EdSkill.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private static readonly string[] UserIdClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        JwtRegisteredClaimNames.Sub,
        "sub",
        "nameid"
    ];

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetUserId()
    {
        var userId = TryGetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        return userId.Value;
    }

    public Guid? TryGetUserId()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = UserIdClaimTypes
            .Select(claimType => principal?.FindFirst(claimType)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}
