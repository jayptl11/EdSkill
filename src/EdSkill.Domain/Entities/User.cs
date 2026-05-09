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
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public decimal TokenBalance { get; set; } = 0m;
    }
}
