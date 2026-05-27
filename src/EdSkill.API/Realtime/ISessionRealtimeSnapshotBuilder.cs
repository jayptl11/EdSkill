namespace EdSkill.API.Realtime;

public interface ISessionRealtimeSnapshotBuilder
{
    Task<bool> CanUserAccessSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<SessionRoomStateSnapshot?> BuildRoomStateSnapshotAsync(Guid sessionId, CancellationToken cancellationToken);
}
