using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class PolicyDocument
{
    public Guid PolicyDocumentId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public PolicyType? PolicyType { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public bool RequiresAcceptance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
