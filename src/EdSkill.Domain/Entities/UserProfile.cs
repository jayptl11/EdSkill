using System;
using System.Collections.Generic;
using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities
{
    public class UserProfile
    {
        public Guid ProfileId { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string DisplayName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public List<string> SkillsToTeach { get; set; } = new();
        public List<string> SkillsToLearn { get; set; } = new();
        public bool IsPublic { get; set; } = true;
        public double ReputationScore { get; set; }
        public int TotalSessions { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public UserGender? Gender { get; set; }
        public string? SocialLinkUrl { get; set; }
        public string? DegreeUrl { get; set; }
        public List<string> CredentialUrls { get; set; } = new();
        public string? Address { get; set; }
    }
}
