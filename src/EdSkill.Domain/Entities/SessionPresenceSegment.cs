namespace EdSkill.Domain.Entities;

public class SessionPresenceSegment
{
    public Guid SessionPresenceSegmentId { get; set; }
    public Guid SessionId { get; set; }
    public virtual Session Session { get; set; } = null!;
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}
