using System.Text.Json;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.System;
using EdSkill.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EdSkill.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IApplicationDbContext
    {
        private static readonly DateTime ConfigSeedTimestamp = new(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime LedgerSeedTimestamp = new(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        private static readonly Guid PlatformLedgerId = Guid.Parse("90000000-0000-0000-0000-000000000001");

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<PointWallet> PointWallets => Set<PointWallet>();
        public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
        public DbSet<SystemLedgerAccount> SystemLedgerAccounts => Set<SystemLedgerAccount>();
        public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();
        public DbSet<PolicyConsent> PolicyConsents => Set<PolicyConsent>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<UserSkill> UserSkills => Set<UserSkill>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<TokenTransaction> TokenTransactions => Set<TokenTransaction>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<TokenBlacklist> TokenBlacklist => Set<TokenBlacklist>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasKey(e => e.UserId);
            modelBuilder.Entity<UserProfile>().HasKey(e => e.ProfileId);
            modelBuilder.Entity<PointWallet>().HasKey(e => e.PointWalletId);
            modelBuilder.Entity<PointTransaction>().HasKey(e => e.PointTransactionId);
            modelBuilder.Entity<Session>().HasKey(e => e.SessionId);
            modelBuilder.Entity<SystemConfig>().HasKey(e => e.Key);
            modelBuilder.Entity<SystemLedgerAccount>().HasKey(e => e.SystemLedgerAccountId);
            modelBuilder.Entity<PolicyDocument>().HasKey(e => e.PolicyDocumentId);
            modelBuilder.Entity<PolicyConsent>().HasKey(e => e.PolicyConsentId);
            modelBuilder.Entity<Skill>().HasKey(e => e.SkillId);
            modelBuilder.Entity<UserSkill>().HasKey(e => e.UserSkillId);
            modelBuilder.Entity<Review>().HasKey(e => e.ReviewId);
            modelBuilder.Entity<TokenTransaction>().HasKey(e => e.TokenTransactionId);
            modelBuilder.Entity<RefreshToken>().HasKey(e => e.TokenId);
            modelBuilder.Entity<TokenBlacklist>().HasKey(e => e.Id);

            var stringListConverter = new ValueConverter<List<string>, string>(
                values => JsonSerializer.Serialize(values, (JsonSerializerOptions?)null),
                values => JsonSerializer.Deserialize<List<string>>(values, (JsonSerializerOptions?)null) ?? new List<string>());

            var stringListComparer = new ValueComparer<List<string>>(
                (left, right) => (left ?? new List<string>()).SequenceEqual(right ?? new List<string>()),
                values => (values ?? new List<string>())
                    .Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
                values => (values ?? new List<string>()).ToList());

            var intListConverter = new ValueConverter<List<int>, string>(
                values => JsonSerializer.Serialize(values, (JsonSerializerOptions?)null),
                values => JsonSerializer.Deserialize<List<int>>(values, (JsonSerializerOptions?)null) ?? new List<int>());

            var intListComparer = new ValueComparer<List<int>>(
                (left, right) => (left ?? new List<int>()).SequenceEqual(right ?? new List<int>()),
                values => (values ?? new List<int>())
                    .Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
                values => (values ?? new List<int>()).ToList());

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var primaryKey = entityType.FindPrimaryKey();
                if (primaryKey != null && primaryKey.Properties.Count == 1)
                {
                    var pkProperty = primaryKey.Properties[0];
                    if (pkProperty.ClrType == typeof(Guid))
                    {
                        modelBuilder.Entity(entityType.ClrType)
                            .Property(pkProperty.Name)
                            .ValueGeneratedOnAdd();
                    }
                }
            }

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.TokenBalance).HasPrecision(18, 2);
                entity.Property(u => u.Status)
                    .HasMaxLength(32)
                    .HasDefaultValue("active");

                var rolesConverter = new ValueConverter<List<string>, string>(
                    roles => JsonSerializer.Serialize(roles, (JsonSerializerOptions?)null),
                    roles => JsonSerializer.Deserialize<List<string>>(roles, (JsonSerializerOptions?)null) ?? new List<string>());

                var rolesComparer = new ValueComparer<List<string>>(
                    (left, right) => left!.SequenceEqual(right!),
                    roles => roles.Aggregate(0, (hash, role) => HashCode.Combine(hash, role.GetHashCode())),
                    roles => roles.ToList());

                entity.Property(u => u.Roles)
                    .HasConversion(rolesConverter)
                    .HasColumnType("nvarchar(max)")
                    .HasDefaultValueSql("N'[\"learner\"]'")
                    .Metadata.SetValueComparer(rolesComparer);

                entity.HasOne(u => u.UserProfile)
                    .WithOne(p => p.User)
                    .HasForeignKey<UserProfile>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(u => u.PointWallet)
                    .WithOne(wallet => wallet.User)
                    .HasForeignKey<PointWallet>(wallet => wallet.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.UserSkills)
                    .WithOne(us => us.User)
                    .HasForeignKey(us => us.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.PolicyConsents)
                    .WithOne(consent => consent.User)
                    .HasForeignKey(consent => consent.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.PointTransactions)
                    .WithOne(transaction => transaction.User)
                    .HasForeignKey(transaction => transaction.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.TokenTransactions)
                    .WithOne(transaction => transaction.User)
                    .HasForeignKey(transaction => transaction.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.CompanionSessions)
                    .WithOne(session => session.Companion)
                    .HasForeignKey(session => session.CompanionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.LearnerSessions)
                    .WithOne(session => session.Learner)
                    .HasForeignKey(session => session.LearnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.UpdatedSystemConfigs)
                    .WithOne(config => config.UpdatedByUser)
                    .HasForeignKey(config => config.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasIndex(p => p.UserId).IsUnique();
                entity.Property(p => p.DisplayName)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.Property(p => p.Bio)
                    .HasMaxLength(500);
                entity.Property(p => p.AvatarUrl)
                    .HasMaxLength(2048);
                entity.Property(p => p.DegreeUrl)
                    .HasMaxLength(2048);
                entity.Property(p => p.CredentialUrls)
                    .HasConversion(stringListConverter)
                    .HasColumnType("nvarchar(max)")
                    .HasDefaultValueSql("N'[]'")
                    .Metadata.SetValueComparer(stringListComparer);
                entity.Property(p => p.Phone)
                    .HasMaxLength(50);
                entity.Property(p => p.Address)
                    .HasMaxLength(500);
                entity.Property(p => p.IsPublic)
                    .HasDefaultValue(true);
                entity.Property(p => p.ReputationScore)
                    .HasDefaultValue(0d);
                entity.Property(p => p.TotalSessions)
                    .HasDefaultValue(0);
                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(p => p.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(p => p.SkillsToTeach)
                    .HasConversion(stringListConverter)
                    .HasColumnType("nvarchar(max)")
                    .HasDefaultValueSql("N'[]'")
                    .Metadata.SetValueComparer(stringListComparer);

                entity.Property(p => p.SkillsToLearn)
                    .HasConversion(stringListConverter)
                    .HasColumnType("nvarchar(max)")
                    .HasDefaultValueSql("N'[]'")
                    .Metadata.SetValueComparer(stringListComparer);
            });

            modelBuilder.Entity<PointWallet>(entity =>
            {
                entity.HasIndex(wallet => wallet.UserId).IsUnique();
                entity.Property(wallet => wallet.Balance).HasDefaultValue(0);
                entity.Property(wallet => wallet.HeldBalance).HasDefaultValue(0);
                entity.Property(wallet => wallet.TotalEarned).HasDefaultValue(0);
                entity.Property(wallet => wallet.TotalSpent).HasDefaultValue(0);
                entity.Property(wallet => wallet.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(wallet => wallet.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<SystemLedgerAccount>(entity =>
            {
                entity.HasIndex(account => account.Code).IsUnique();
                entity.Property(account => account.Code).HasMaxLength(64).IsRequired();
                entity.Property(account => account.Name).HasMaxLength(200).IsRequired();
                entity.Property(account => account.Balance).HasDefaultValue(0);
                entity.Property(account => account.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(account => account.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasData(new SystemLedgerAccount
                {
                    SystemLedgerAccountId = PlatformLedgerId,
                    Code = SystemLedgerAccountCodes.PlatformFee,
                    Name = "Platform Fee Ledger",
                    Balance = 0,
                    CreatedAt = LedgerSeedTimestamp,
                    UpdatedAt = LedgerSeedTimestamp
                });
            });

            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.Property(config => config.Key).HasMaxLength(128);
                entity.Property(config => config.Value).HasMaxLength(256).IsRequired();
                entity.Property(config => config.Description).HasMaxLength(500).IsRequired();
                entity.Property(config => config.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasData(SystemConfigCatalog.CreateSeed(null, ConfigSeedTimestamp));
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.Property(session => session.Skill).HasMaxLength(100).IsRequired();
                entity.Property(session => session.Description).HasMaxLength(2000);
                entity.Property(session => session.DeliveryMode)
                    .HasConversion<string>()
                    .HasMaxLength(32)
                    .HasDefaultValue(Domain.Enums.SessionDeliveryMode.Online);
                entity.Property(session => session.PricingModel)
                    .HasConversion<string>()
                    .HasMaxLength(32)
                    .HasDefaultValue(Domain.Enums.SessionPricingModel.LegacyManual);
                entity.Property(session => session.DurationOptions)
                    .HasConversion(intListConverter)
                    .HasColumnType("nvarchar(max)")
                    .HasDefaultValueSql("N'[]'")
                    .Metadata.SetValueComparer(intListComparer);
                entity.Property(session => session.Location).HasMaxLength(500);
                entity.Property(session => session.Status)
                    .HasConversion<string>()
                    .HasMaxLength(32);
                entity.Property(session => session.JitsiRoomId).HasMaxLength(200);
                entity.Property(session => session.CancelReason).HasMaxLength(500);
                entity.Property(session => session.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(session => session.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(session => new { session.CompanionId, session.ScheduledAt });
                entity.HasIndex(session => session.SkillId);
                entity.HasIndex(session => session.Status);
            });

            modelBuilder.Entity<PointTransaction>(entity =>
            {
                entity.Property(transaction => transaction.Type)
                    .HasConversion<string>()
                    .HasMaxLength(32);
                entity.Property(transaction => transaction.Note).HasMaxLength(500);
                entity.Property(transaction => transaction.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(transaction => new { transaction.UserId, transaction.CreatedAt });
                entity.HasIndex(transaction => new { transaction.SessionId, transaction.CreatedAt });

                entity.HasOne(transaction => transaction.SystemLedgerAccount)
                    .WithMany()
                    .HasForeignKey(transaction => transaction.SystemLedgerAccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(transaction => transaction.Session)
                    .WithMany()
                    .HasForeignKey(transaction => transaction.SessionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PolicyDocument>(entity =>
            {
                entity.HasIndex(document => new { document.Slug, document.Version }).IsUnique();
                entity.Property(document => document.Slug)
                    .HasMaxLength(120)
                    .IsRequired();
                entity.Property(document => document.Category)
                    .HasMaxLength(64)
                    .IsRequired();
                entity.Property(document => document.Audience)
                    .HasMaxLength(32)
                    .IsRequired();
                entity.Property(document => document.PolicyType)
                    .HasConversion<string>()
                    .HasMaxLength(64);
                entity.Property(document => document.Version)
                    .HasMaxLength(32)
                    .IsRequired();
                entity.Property(document => document.Title)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(document => document.Summary)
                    .HasMaxLength(500)
                    .IsRequired();
                entity.Property(document => document.ContentMarkdown)
                    .HasColumnType("nvarchar(max)")
                    .IsRequired();
                entity.Property(document => document.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(document => document.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasData(PolicySeedData.Documents);
            });

            modelBuilder.Entity<PolicyConsent>(entity =>
            {
                entity.HasIndex(consent => new { consent.UserId, consent.PolicyType, consent.PolicyVersion }).IsUnique();
                entity.Property(consent => consent.PolicyType)
                    .HasConversion<string>()
                    .HasMaxLength(64);
                entity.Property(consent => consent.PolicyVersion)
                    .HasMaxLength(32)
                    .IsRequired();
            });

            modelBuilder.Entity<Skill>(entity =>
            {
                entity.HasIndex(s => s.Name).IsUnique();
                entity.HasIndex(s => s.Slug).IsUnique();
                entity.Property(s => s.Name)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.Property(s => s.Slug)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(s => s.Category)
                    .HasMaxLength(100);
                entity.Property(s => s.BasePointCost)
                    .HasDefaultValue(0);
                entity.Property(s => s.Aliases)
                    .HasConversion(stringListConverter)
                    .HasColumnType("nvarchar(max)")
                    .HasDefaultValueSql("N'[]'")
                    .Metadata.SetValueComparer(stringListComparer);
                entity.Property(s => s.IsActive)
                    .HasDefaultValue(true);
                entity.Property(s => s.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(s => s.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<UserSkill>(entity =>
            {
                entity.HasIndex(us => new { us.UserId, us.SkillId, us.Type }).IsUnique();
                entity.Property(us => us.Type)
                    .HasConversion<string>()
                    .HasMaxLength(16);
                entity.Property(us => us.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(us => us.Skill)
                    .WithMany(s => s.UserSkills)
                    .HasForeignKey(us => us.SkillId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasIndex(review => new { review.SessionId, review.ReviewerId }).IsUnique();
                entity.HasIndex(review => new { review.RevieweeId, review.CreatedAt });
                entity.Property(review => review.Rating).IsRequired();
                entity.Property(review => review.Comment).HasMaxLength(1000);
                entity.Property(review => review.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(review => review.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(review => review.Session)
                    .WithMany()
                    .HasForeignKey(review => review.SessionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TokenTransaction>(entity =>
            {
                entity.Property(transaction => transaction.Type)
                    .HasConversion<string>()
                    .HasMaxLength(64);
                entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
                entity.Property(transaction => transaction.BalanceBefore).HasPrecision(18, 2);
                entity.Property(transaction => transaction.BalanceAfter).HasPrecision(18, 2);
                entity.Property(transaction => transaction.Note).HasMaxLength(500);
                entity.Property(transaction => transaction.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(transaction => new { transaction.UserId, transaction.CreatedAt });
                entity.HasIndex(transaction => new { transaction.SessionId, transaction.CreatedAt });

                entity.HasOne(transaction => transaction.Session)
                    .WithMany()
                    .HasForeignKey(transaction => transaction.SessionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
