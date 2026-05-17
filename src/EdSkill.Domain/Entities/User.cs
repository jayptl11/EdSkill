using System;
using System.Collections.Generic;

namespace EdSkill.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public string? Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new() { "learner" };
        public virtual UserProfile? UserProfile { get; set; }
        public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<PolicyConsent> PolicyConsents { get; set; } = new List<PolicyConsent>();
        public virtual PointWallet? PointWallet { get; set; }
        public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
        public virtual ICollection<TokenTransaction> TokenTransactions { get; set; } = new List<TokenTransaction>();
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
        public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
        public virtual ICollection<Session> CompanionSessions { get; set; } = new List<Session>();
        public virtual ICollection<Session> LearnerSessions { get; set; } = new List<Session>();
        public virtual ICollection<SystemConfig> UpdatedSystemConfigs { get; set; } = new List<SystemConfig>();
        public decimal TokenBalance { get; set; } = 0m;
    }
}
