namespace EdSkill.Domain.Entities;

public class Review
{
    public Guid ReviewId { get; set; }
    public Guid SessionId { get; set; }
    public virtual Session Session { get; set; } = null!;
    public Guid ReviewerId { get; set; }
    public Guid RevieweeId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
