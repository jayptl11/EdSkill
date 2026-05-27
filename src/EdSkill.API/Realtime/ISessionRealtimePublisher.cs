using EdSkill.Application.Features.Sessions.DTOs;

namespace EdSkill.API.Realtime;

public interface ISessionRealtimePublisher
{
    Task PublishSessionUpdatedAsync(SessionDto session, CancellationToken cancellationToken);
    Task PublishRoomStateUpdatedAsync(Guid sessionId, CancellationToken cancellationToken);
}
