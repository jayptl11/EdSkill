using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.Application.Features.Skills;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Companions;

internal static class CompanionSessionFilters
{
    public static async Task<List<Session>> ApplyAsync(
        IQueryable<Session> query,
        Skill skill,
        SessionDeliveryMode? deliveryMode,
        string? location,
        CancellationToken cancellationToken)
    {
        query = query.Where(session => session.Status == SessionStatus.Available);

        if (deliveryMode.HasValue)
        {
            query = query.Where(session => session.DeliveryMode == deliveryMode.Value);
        }

        if (deliveryMode == SessionDeliveryMode.Offline && !string.IsNullOrWhiteSpace(location))
        {
            var normalizedLocation = location.Trim().ToLowerInvariant();
            query = query.Where(session =>
                session.Location != null &&
                session.Location.ToLower().Contains(normalizedLocation));
        }

        var sessions = await query.ToListAsync(cancellationToken);
        var validSkillKeys = BuildSkillKeys(skill);

        return sessions
            .Where(session =>
                session.SkillId == skill.SkillId
                || validSkillKeys.Contains(SkillNormalization.NormalizeLookup(session.Skill)))
            .ToList();
    }

    private static HashSet<string> BuildSkillKeys(Skill skill)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal)
        {
            SkillNormalization.NormalizeLookup(skill.Name),
            SkillNormalization.NormalizeLookup(skill.Slug)
        };

        foreach (var alias in skill.Aliases)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                keys.Add(SkillNormalization.NormalizeLookup(alias));
            }
        }

        return keys;
    }
}
