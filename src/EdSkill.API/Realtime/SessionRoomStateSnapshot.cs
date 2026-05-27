using EdSkill.Application.Features.Sessions.DTOs;

namespace EdSkill.API.Realtime;

public record SessionRoomStateSnapshot(
    SessionRoomStateDto Payload,
    IReadOnlyCollection<string> UserGroups
);
