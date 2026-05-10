using EdSkill.Application.Common.Interfaces;
using EdSkill.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace EdSkill.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IApplicationDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<TokenBlacklist> TokenBlacklist => Set<TokenBlacklist>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasKey(e => e.UserId);
            modelBuilder.Entity<UserProfile>().HasKey(e => e.ProfileId);
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
                entity.Property(p => p.University)
                    .HasMaxLength(200);
                entity.Property(p => p.Faculty)
                    .HasMaxLength(200);
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
        }
    }
}
