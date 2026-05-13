using EdSkill.Application.Common.Models;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Common.Interfaces;

public interface ISessionPricingService
{
    Task<int> GetPlatformMarkupPctAsync(CancellationToken cancellationToken);
    Result<SessionPricingPreview> BuildOfferPreview(Skill skill, int credentialCount, IReadOnlyCollection<int> durationOptions, int platformMarkupPct);
    Result<FormulaSessionPricingSnapshot> BuildBookingSnapshot(Skill skill, int credentialCount, int selectedDurationMinutes, int platformMarkupPct);
}
