using EdSkill.Domain.Enums;
using EdSkill.Application.Features.Sessions.DTOs;

namespace EdSkill.Application.Features.MySpace.DTOs;

public record MySpaceSkillDto(
    Guid SkillId,
    string Name,
    string? IconKey
);

public record MySpaceUserSummaryDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl
);

public record MySpaceRoomAccessDto(
    bool CanOpenRoomPage,
    bool CanJoinNow,
    bool HostReady,
    bool HasCompanionJoined,
    string? DenyCode,
    string? DenyMessage,
    DateTime JoinOpenAt,
    DateTime JoinCloseAt
);

public record MySpaceSessionDto(
    SessionDto Session,
    MySpaceSkillDto? Skill,
    MySpaceUserSummaryDto Companion,
    MySpaceUserSummaryDto? Learner,
    MySpaceRoomAccessDto? RoomAccess
);

public record MySpaceDto(
    IReadOnlyCollection<MySpaceSessionDto> CompanionSessions,
    IReadOnlyCollection<MySpaceSessionDto> LearnerSessions
);
