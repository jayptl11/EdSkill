using System;
using System.Collections.Generic;

namespace EdSkill.Domain.Entities;

public class Skill
{
    public Guid SkillId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? IconKey { get; set; }
    public int BasePointCost { get; set; }
    public List<string> Aliases { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
}
