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
        private static readonly DateTime PointPackageSeedTimestamp = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SubscriptionPlanSeedTimestamp = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        private static readonly Guid PlatformLedgerId = Guid.Parse("90000000-0000-0000-0000-000000000001");
        private static readonly Guid PointPackage1Id = Guid.Parse("91000000-0000-0000-0000-000000000001");
        private static readonly Guid PointPackage2Id = Guid.Parse("91000000-0000-0000-0000-000000000002");
        private static readonly Guid PointPackage3Id = Guid.Parse("91000000-0000-0000-0000-000000000003");
        private static readonly Guid PointPackage4Id = Guid.Parse("91000000-0000-0000-0000-000000000004");
        private static readonly Guid LearnerProPlanId = Guid.Parse("92000000-0000-0000-0000-000000000001");
        private static readonly Guid CompanionProPlanId = Guid.Parse("92000000-0000-0000-0000-000000000002");
        private static readonly Guid MultiRoleProPlanId = Guid.Parse("92000000-0000-0000-0000-000000000003");

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<PointWallet> PointWallets => Set<PointWallet>();
        public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
        public DbSet<PointPackage> PointPackages => Set<PointPackage>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<SessionPresenceSegment> SessionPresenceSegments => Set<SessionPresenceSegment>();
        public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
        public DbSet<SystemLedgerAccount> SystemLedgerAccounts => Set<SystemLedgerAccount>();
        public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();
        public DbSet<PolicyConsent> PolicyConsents => Set<PolicyConsent>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<UserSkill> UserSkills => Set<UserSkill>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<AchievementDefinition> AchievementDefinitions => Set<AchievementDefinition>();
        public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
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
            modelBuilder.Entity<PointPackage>().HasKey(e => e.PointPackageId);
            modelBuilder.Entity<PaymentTransaction>().HasKey(e => e.PaymentTransactionId);
            modelBuilder.Entity<SubscriptionPlan>().HasKey(e => e.SubscriptionPlanId);
            modelBuilder.Entity<UserSubscription>().HasKey(e => e.UserSubscriptionId);
            modelBuilder.Entity<Session>().HasKey(e => e.SessionId);
            modelBuilder.Entity<SessionPresenceSegment>().HasKey(e => e.SessionPresenceSegmentId);
            modelBuilder.Entity<SystemConfig>().HasKey(e => e.Key);
            modelBuilder.Entity<SystemLedgerAccount>().HasKey(e => e.SystemLedgerAccountId);
            modelBuilder.Entity<PolicyDocument>().HasKey(e => e.PolicyDocumentId);
            modelBuilder.Entity<PolicyConsent>().HasKey(e => e.PolicyConsentId);
            modelBuilder.Entity<Skill>().HasKey(e => e.SkillId);
            modelBuilder.Entity<UserSkill>().HasKey(e => e.UserSkillId);
            modelBuilder.Entity<Review>().HasKey(e => e.ReviewId);
            modelBuilder.Entity<AchievementDefinition>().HasKey(e => e.AchievementDefinitionId);
            modelBuilder.Entity<UserAchievement>().HasKey(e => e.UserAchievementId);
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

                entity.HasMany(u => u.PaymentTransactions)
                    .WithOne(transaction => transaction.User)
                    .HasForeignKey(transaction => transaction.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.UserSubscriptions)
                    .WithOne(subscription => subscription.User)
                    .HasForeignKey(subscription => subscription.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.TokenTransactions)
                    .WithOne(transaction => transaction.User)
                    .HasForeignKey(transaction => transaction.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.UserAchievements)
                    .WithOne(achievement => achievement.User)
                    .HasForeignKey(achievement => achievement.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.CompanionSessions)
                    .WithOne(session => session.Companion)
                    .HasForeignKey(session => session.CompanionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.LearnerSessions)
                    .WithOne(session => session.Learner)
                    .HasForeignKey(session => session.LearnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.SessionPresenceSegments)
                    .WithOne(segment => segment.User)
                    .HasForeignKey(segment => segment.UserId)
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
                entity.Property(p => p.Gender)
                    .HasConversion<string>()
                    .HasMaxLength(32);
                entity.Property(p => p.SocialLinkUrl)
                    .HasMaxLength(2048);
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

                entity.HasMany(session => session.PresenceSegments)
                    .WithOne(segment => segment.Session)
                    .HasForeignKey(segment => segment.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SessionPresenceSegment>(entity =>
            {
                entity.Property(segment => segment.JoinedAt).IsRequired();
                entity.Property(segment => segment.LeftAt);
                entity.HasIndex(segment => new { segment.SessionId, segment.UserId, segment.JoinedAt });
                entity.HasIndex(segment => new { segment.SessionId, segment.UserId })
                    .IsUnique()
                    .HasFilter("[LeftAt] IS NULL");
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

            modelBuilder.Entity<PointPackage>(entity =>
            {
                entity.HasIndex(package => package.Code).IsUnique();
                entity.HasIndex(package => new { package.IsDeleted, package.IsActive, package.DisplayOrder });
                entity.Property(package => package.Code).HasMaxLength(64).IsRequired();
                entity.Property(package => package.Name).HasMaxLength(100).IsRequired();
                entity.Property(package => package.Currency).HasMaxLength(8).IsRequired();
                entity.Property(package => package.Description).HasMaxLength(500);
                entity.Property(package => package.BadgeText).HasMaxLength(100);
                entity.Property(package => package.BonusPoints).HasDefaultValue(0);
                entity.Property(package => package.Currency).HasDefaultValue("VND");
                entity.Property(package => package.IsActive).HasDefaultValue(true);
                entity.Property(package => package.IsDeleted).HasDefaultValue(false);
                entity.Property(package => package.IsHighlighted).HasDefaultValue(false);
                entity.Property(package => package.DisplayOrder).HasDefaultValue(0);
                entity.Property(package => package.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(package => package.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasData(
                    new PointPackage
                    {
                        PointPackageId = PointPackage1Id,
                        Code = "goi_1",
                        Name = "Gói 1",
                        Points = 500,
                        BonusPoints = 0,
                        PriceVnd = 59000,
                        Currency = "VND",
                        Description = "Gói nạp 500 Points.",
                        DisplayOrder = 1,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = PointPackageSeedTimestamp,
                        UpdatedAt = PointPackageSeedTimestamp
                    },
                    new PointPackage
                    {
                        PointPackageId = PointPackage2Id,
                        Code = "goi_2",
                        Name = "Gói 2",
                        Points = 1000,
                        BonusPoints = 0,
                        PriceVnd = 99000,
                        Currency = "VND",
                        Description = "Gói nạp 1.000 Points.",
                        DisplayOrder = 2,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = PointPackageSeedTimestamp,
                        UpdatedAt = PointPackageSeedTimestamp
                    },
                    new PointPackage
                    {
                        PointPackageId = PointPackage3Id,
                        Code = "goi_3",
                        Name = "Gói 3",
                        Points = 2000,
                        BonusPoints = 0,
                        PriceVnd = 169000,
                        Currency = "VND",
                        Description = "Gói nạp 2.000 Points.",
                        DisplayOrder = 3,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = PointPackageSeedTimestamp,
                        UpdatedAt = PointPackageSeedTimestamp
                    },
                    new PointPackage
                    {
                        PointPackageId = PointPackage4Id,
                        Code = "goi_4",
                        Name = "Gói 4",
                        Points = 5000,
                        BonusPoints = 0,
                        PriceVnd = 379000,
                        Currency = "VND",
                        Description = "Gói nạp 5.000 Points.",
                        DisplayOrder = 4,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = PointPackageSeedTimestamp,
                        UpdatedAt = PointPackageSeedTimestamp
                    });
            });

            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.Property(transaction => transaction.Provider)
                    .HasConversion<string>()
                    .HasMaxLength(32);
                entity.Property(transaction => transaction.ProviderTransactionId).HasMaxLength(128);
                entity.Property(transaction => transaction.Currency).HasMaxLength(8).IsRequired();
                entity.Property(transaction => transaction.Status)
                    .HasConversion<string>()
                    .HasMaxLength(32);
                entity.Property(transaction => transaction.PaymentUrl).HasMaxLength(2048);
                entity.Property(transaction => transaction.RawPayload).HasColumnType("nvarchar(max)");
                entity.Property(transaction => transaction.Currency).HasDefaultValue("VND");
                entity.Property(transaction => transaction.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(transaction => transaction.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(transaction => new { transaction.UserId, transaction.Status, transaction.CreatedAt });
                entity.HasIndex(transaction => new { transaction.Provider, transaction.ProviderTransactionId })
                    .IsUnique()
                    .HasFilter("[ProviderTransactionId] IS NOT NULL AND [Status] = 'Success'");

                entity.HasOne(transaction => transaction.PointPackage)
                    .WithMany(package => package.PaymentTransactions)
                    .HasForeignKey(transaction => transaction.PointPackageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(transaction => transaction.SubscriptionPlan)
                    .WithMany(plan => plan.PaymentTransactions)
                    .HasForeignKey(transaction => transaction.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasIndex(plan => plan.Code).IsUnique();
                entity.HasIndex(plan => new { plan.IsActive, plan.DisplayOrder });
                entity.Property(plan => plan.Code).HasMaxLength(64).IsRequired();
                entity.Property(plan => plan.Name).HasMaxLength(100).IsRequired();
                entity.Property(plan => plan.TargetRole)
                    .HasConversion<string>()
                    .HasMaxLength(32);
                entity.Property(plan => plan.Currency).HasMaxLength(8).IsRequired();
                entity.Property(plan => plan.BillingCycle)
                    .HasConversion<string>()
                    .HasMaxLength(32);
                entity.Property(plan => plan.LearnerTokenRewardRatePercent).HasPrecision(5, 2);
                entity.Property(plan => plan.CompanionTokenRewardRatePercent).HasPrecision(5, 2);
                entity.Property(plan => plan.CompanionBadgeText).HasMaxLength(100);
                entity.Property(plan => plan.BenefitsJson).HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(plan => plan.Currency).HasDefaultValue("VND");
                entity.Property(plan => plan.BillingCycle).HasDefaultValue(Domain.Enums.SubscriptionBillingCycle.Monthly);
                entity.Property(plan => plan.IsActive).HasDefaultValue(true);
                entity.Property(plan => plan.DisplayOrder).HasDefaultValue(0);
                entity.Property(plan => plan.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(plan => plan.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasData(
                    new SubscriptionPlan
                    {
                        SubscriptionPlanId = LearnerProPlanId,
                        Code = "learner_pro",
                        Name = "Learner Pro",
                        TargetRole = Domain.Enums.SubscriptionTargetRole.Learner,
                        PriceVnd = 119000,
                        Currency = "VND",
                        BillingCycle = Domain.Enums.SubscriptionBillingCycle.Monthly,
                        ImmediateBonusPoints = 200,
                        BenefitsJson = "[\"Tang ngay 200 Point\",\"Voucher 75% hang tuan\",\"Khong quang cao\",\"Uu tien matching\",\"Rebook nhanh\"]",
                        IsActive = true,
                        DisplayOrder = 1,
                        CreatedAt = SubscriptionPlanSeedTimestamp,
                        UpdatedAt = SubscriptionPlanSeedTimestamp
                    },
                    new SubscriptionPlan
                    {
                        SubscriptionPlanId = CompanionProPlanId,
                        Code = "companion_pro",
                        Name = "Companion Pro",
                        TargetRole = Domain.Enums.SubscriptionTargetRole.Companion,
                        PriceVnd = 79000,
                        Currency = "VND",
                        BillingCycle = Domain.Enums.SubscriptionBillingCycle.Monthly,
                        CompanionTokenRewardRatePercent = 30m,
                        CompanionDailySessionLimitOverride = 12,
                        CompanionBadgeText = "Companion Pro",
                        HasPriorityVisibility = true,
                        BenefitsJson = "[\"Ed-Token bonus 30%\",\"Ho so noi bat hon\",\"Uu tien hien thi\",\"Mo nhieu slot hon\",\"Dashboard nang cao\"]",
                        IsActive = true,
                        DisplayOrder = 2,
                        CreatedAt = SubscriptionPlanSeedTimestamp,
                        UpdatedAt = SubscriptionPlanSeedTimestamp
                    },
                    new SubscriptionPlan
                    {
                        SubscriptionPlanId = MultiRoleProPlanId,
                        Code = "multi_role_pro",
                        Name = "Da nang Pro",
                        TargetRole = Domain.Enums.SubscriptionTargetRole.MultiRole,
                        PriceVnd = 179000,
                        Currency = "VND",
                        BillingCycle = Domain.Enums.SubscriptionBillingCycle.Monthly,
                        WeeklyLearnerSessionBonusPoints = 200,
                        WeeklyCompanionSessionBonusPoints = 200,
                        LearnerTokenRewardRatePercent = 10m,
                        CompanionTokenRewardRatePercent = 6m,
                        CompanionDailySessionLimitOverride = 12,
                        CompanionBadgeText = "Da nang Pro",
                        HasPriorityVisibility = true,
                        BenefitsJson = "[\"200 Point cho buoi hoc dau tien trong tuan\",\"200 Point cho buoi huong dan dau tien trong tuan\",\"Learner token 10%\",\"Companion token 6%\",\"Bao gom quyen loi Learner va Companion\"]",
                        IsActive = true,
                        DisplayOrder = 3,
                        CreatedAt = SubscriptionPlanSeedTimestamp,
                        UpdatedAt = SubscriptionPlanSeedTimestamp
                    });
            });

            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.Property(subscription => subscription.Status)
                    .HasConversion<string>()
                    .HasMaxLength(32);
                entity.Property(subscription => subscription.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(subscription => subscription.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(subscription => new { subscription.UserId, subscription.Status, subscription.ExpiresAt });
                entity.HasIndex(subscription => subscription.PlanId);
                entity.HasIndex(subscription => subscription.PaymentTransactionId)
                    .IsUnique()
                    .HasFilter("[PaymentTransactionId] IS NOT NULL");

                entity.HasOne(subscription => subscription.Plan)
                    .WithMany(plan => plan.UserSubscriptions)
                    .HasForeignKey(subscription => subscription.PlanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(subscription => subscription.PaymentTransaction)
                    .WithOne(payment => payment.UserSubscription)
                    .HasForeignKey<UserSubscription>(subscription => subscription.PaymentTransactionId)
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
                entity.Property(s => s.IconKey)
                    .HasMaxLength(50);
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

            modelBuilder.Entity<AchievementDefinition>(entity =>
            {
                entity.HasIndex(achievement => achievement.Name).IsUnique();
                entity.Property(achievement => achievement.Name)
                    .HasMaxLength(120)
                    .IsRequired();
                entity.Property(achievement => achievement.Description)
                    .HasMaxLength(500)
                    .IsRequired();
                entity.Property(achievement => achievement.IconUrl)
                    .HasMaxLength(2048);
                entity.Property(achievement => achievement.Track)
                    .HasConversion<string>()
                    .HasMaxLength(32);
                entity.Property(achievement => achievement.Metric)
                    .HasConversion<string>()
                    .HasMaxLength(64);
                entity.Property(achievement => achievement.Threshold)
                    .HasDefaultValue(1);
                entity.Property(achievement => achievement.SortOrder)
                    .HasDefaultValue(0);
                entity.Property(achievement => achievement.IsActive)
                    .HasDefaultValue(true);
                entity.Property(achievement => achievement.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(achievement => achievement.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(achievement => achievement.EffectiveFromUtc).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<UserAchievement>(entity =>
            {
                entity.HasIndex(achievement => new { achievement.UserId, achievement.AchievementDefinitionId }).IsUnique();
                entity.HasIndex(achievement => new { achievement.UserId, achievement.AwardedAt });
                entity.Property(achievement => achievement.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(achievement => achievement.AwardedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(achievement => achievement.AchievementDefinition)
                    .WithMany(definition => definition.UserAchievements)
                    .HasForeignKey(achievement => achievement.AchievementDefinitionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
