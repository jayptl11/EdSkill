using CodeNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeNexus.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        public DbSet<Role> Roles { get; }
        public DbSet<User> Users { get; }
        public DbSet<UserProfile> UserProfiles { get; }
        public DbSet<RefreshToken> RefreshTokens { get; }
        public DbSet<TokenBlacklist> TokenBlacklist { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
