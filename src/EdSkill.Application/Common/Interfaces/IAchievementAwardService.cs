using EdSkill.Domain.Entities;

namespace EdSkill.Application.Common.Interfaces;

public interface IAchievementAwardService
{
    Task AwardForCompletedSessionAsync(Session session, CancellationToken cancellationToken = default);
}
