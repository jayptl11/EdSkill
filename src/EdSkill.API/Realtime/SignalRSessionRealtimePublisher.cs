using EdSkill.API.Hubs;
using EdSkill.Application.Features.Sessions.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace EdSkill.API.Realtime;

public class SignalRSessionRealtimePublisher : ISessionRealtimePublisher
{
    private readonly IHubContext<SessionRealtimeHub> _hubContext;
    private readonly ISessionRealtimeSnapshotBuilder _snapshotBuilder;
    private readonly ILogger<SignalRSessionRealtimePublisher> _logger;

    public SignalRSessionRealtimePublisher(
        IHubContext<SessionRealtimeHub> hubContext,
        ISessionRealtimeSnapshotBuilder snapshotBuilder,
        ILogger<SignalRSessionRealtimePublisher> logger)
    {
        _hubContext = hubContext;
        _snapshotBuilder = snapshotBuilder;
        _logger = logger;
    }

    public async Task PublishSessionUpdatedAsync(SessionDto session, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients
                .Groups(SessionRealtimeGroupNames.ParticipantUsers(session.CompanionId, session.LearnerId))
                .SendAsync(SessionRealtimeEventNames.SessionUpdated, session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish session update for session {SessionId}", session.SessionId);
        }
    }

    public async Task PublishRoomStateUpdatedAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotBuilder.BuildRoomStateSnapshotAsync(sessionId, cancellationToken);
        if (snapshot == null)
        {
            return;
        }

        var groups = snapshot.UserGroups
            .Append(SessionRealtimeGroupNames.Session(sessionId))
            .Distinct()
            .ToArray();

        try
        {
            await _hubContext.Clients
                .Groups(groups)
                .SendAsync(SessionRealtimeEventNames.SessionRoomStateUpdated, snapshot.Payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish room state update for session {SessionId}", sessionId);
        }
    }
}
