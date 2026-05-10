using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.Companions;

internal static class CompanionSessionFilters
{
    public static IQueryable<Session> Apply(
        IQueryable<Session> query,
        string skillName,
        SessionDeliveryMode? deliveryMode,
        string? location)
    {
        query = query.Where(session => session.Status == SessionStatus.Available && session.Skill == skillName);

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

        return query;
    }
}
