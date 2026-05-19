using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Application.Features.Sessions;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.MySpace;

internal static class MySpaceDtoMapper
{
    public static MySpaceDto Map(
        IReadOnlyCollection<MySpaceSessionDto> companionSessions,
        IReadOnlyCollection<MySpaceSessionDto> learnerSessions)
    {
        return new MySpaceDto(
            companionSessions,
            learnerSessions);
    }

    public static MySpaceSessionDto MapSession(
        Session session,
        Skill? skill,
        UserProfile? companionProfile,
        int? platformMarkupPct,
        IDictionary<Guid, MySpaceUserSummaryDto> userLookup)
    {
        var companion = ResolveUser(userLookup, session.CompanionId);
        var learner = session.LearnerId.HasValue
            ? ResolveUser(userLookup, session.LearnerId.Value)
            : null;

        return new MySpaceSessionDto(
            SessionDtoMapper.Map(session, skill, companionProfile, platformMarkupPct),
            skill is null ? null : new MySpaceSkillDto(skill.SkillId, skill.Name, skill.IconKey),
            companion,
            learner);
    }

    private static MySpaceUserSummaryDto ResolveUser(
        IDictionary<Guid, MySpaceUserSummaryDto> userLookup,
        Guid userId)
    {
        return userLookup.TryGetValue(userId, out var user)
            ? user
            : new MySpaceUserSummaryDto(userId, "Unknown", null);
    }
}
