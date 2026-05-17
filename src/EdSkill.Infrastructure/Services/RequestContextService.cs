using System.Net;
using EdSkill.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EdSkill.Infrastructure.Services;

public class RequestContextService : IRequestContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return "127.0.0.1";
        }

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var candidate = forwardedFor
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (IsValidIpAddress(candidate))
            {
                return candidate!;
            }
        }

        var remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        return IsValidIpAddress(remoteIpAddress) ? remoteIpAddress! : "127.0.0.1";
    }

    private static bool IsValidIpAddress(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value, out _);
    }
}
