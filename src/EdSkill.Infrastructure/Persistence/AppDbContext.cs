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
        }
    }
}
