using EdSkill.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        public DbSet<User> Users { get; }
        public DbSet<UserProfile> UserProfiles { get; }
        public DbSet<PointWallet> PointWallets { get; }
        public DbSet<PointTransaction> PointTransactions { get; }
        public DbSet<PointPackage> PointPackages { get; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; }
        public DbSet<UserSubscription> UserSubscriptions { get; }
        public DbSet<Session> Sessions { get; }
        public DbSet<SessionPresenceSegment> SessionPresenceSegments { get; }
        public DbSet<SystemConfig> SystemConfigs { get; }
        public DbSet<SystemLedgerAccount> SystemLedgerAccounts { get; }
        public DbSet<PolicyDocument> PolicyDocuments { get; }
        public DbSet<PolicyConsent> PolicyConsents { get; }
        public DbSet<Skill> Skills { get; }
        public DbSet<UserSkill> UserSkills { get; }
        public DbSet<Review> Reviews { get; }
        public DbSet<AchievementDefinition> AchievementDefinitions { get; }
        public DbSet<UserAchievement> UserAchievements { get; }
        public DbSet<TokenTransaction> TokenTransactions { get; }
        public DbSet<RefreshToken> RefreshTokens { get; }
        public DbSet<TokenBlacklist> TokenBlacklist { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
