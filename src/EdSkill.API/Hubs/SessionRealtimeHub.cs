using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EdSkill.API.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EdSkill.API.Hubs;

[Authorize]
public class SessionRealtimeHub : Hub
{
    private static readonly string[] UserIdClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        JwtRegisteredClaimNames.Sub,
        "sub",
        "nameid"
    ];

    private readonly ISessionRealtimeSnapshotBuilder _snapshotBuilder;

    public SessionRealtimeHub(ISessionRealtimeSnapshotBuilder snapshotBuilder)
    {
        _snapshotBuilder = snapshotBuilder;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, SessionRealtimeGroupNames.User(userId));
        await base.OnConnectedAsync();
    }

    public async Task SubscribeSession(Guid sessionId)
    {
        var userId = GetCurrentUserId();
        var canAccess = await _snapshotBuilder.CanUserAccessSessionAsync(sessionId, userId, Context.ConnectionAborted);
        if (!canAccess)
        {
            throw new HubException("FORBIDDEN");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SessionRealtimeGroupNames.Session(sessionId), Context.ConnectionAborted);
    }

    public Task UnsubscribeSession(Guid sessionId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionRealtimeGroupNames.Session(sessionId), Context.ConnectionAborted);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = UserIdClaimTypes
            .Select(claimType => Context.User?.FindFirst(claimType)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("UNAUTHORIZED");
        }

        return userId;
    }
}
