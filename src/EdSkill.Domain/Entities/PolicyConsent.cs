using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class PolicyConsent
{
    public Guid PolicyConsentId { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public PolicyType PolicyType { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; }
}
