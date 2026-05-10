using System;
using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class UserSkill
{
    public Guid UserSkillId { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public Guid SkillId { get; set; }
    public virtual Skill Skill { get; set; } = null!;
    public UserSkillType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
