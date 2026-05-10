namespace EdSkill.Domain.Entities;

public class SystemConfig
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public virtual User? UpdatedByUser { get; set; }
}
