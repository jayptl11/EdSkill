using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class CompanionSpaceCard
{
    public Guid CompanionSpaceCardId { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public Guid SkillId { get; set; }
    public virtual Skill Skill { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PricePoints { get; set; }
    public int DurationMinutes { get; set; }
    public List<SessionDeliveryMode> DeliveryModes { get; set; } = new();
    public List<string> Languages { get; set; } = new();
    public string? CoverImageUrl { get; set; }
    public List<string> CredentialUrls { get; set; } = new();
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
