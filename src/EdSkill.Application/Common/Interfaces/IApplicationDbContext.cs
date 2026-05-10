using EdSkill.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        public DbSet<User> Users { get; }
        public DbSet<UserProfile> UserProfiles { get; }
        public DbSet<Skill> Skills { get; }
        public DbSet<UserSkill> UserSkills { get; }
        public DbSet<RefreshToken> RefreshTokens { get; }
        public DbSet<TokenBlacklist> TokenBlacklist { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
