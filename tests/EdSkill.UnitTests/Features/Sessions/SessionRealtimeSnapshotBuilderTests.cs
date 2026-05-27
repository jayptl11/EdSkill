using EdSkill.API.Realtime;
using EdSkill.Application.Common.Services;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.UnitTests.Features.Sessions;

public class SessionRealtimeSnapshotBuilderTests
{
    [Fact]
    public async Task BuildRoomStateSnapshotAsync_WhenSessionOnline_ReturnsPresenceAndJoinWindow()
    {
        var databaseName = $"EdSkillSignalRUnit_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var scheduledAt = new DateTime(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 5, 26, 10, 5, 0, DateTimeKind.Utc);

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureDeletedAsync();
            await seedContext.Database.EnsureCreatedAsync();

            seedContext.Users.AddRange(
                CreateUser(companionId, "companion-1", "companion"),
                CreateUser(learnerId, "learner-1", "learner"));
            seedContext.Sessions.Add(new Session
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Realtime",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = scheduledAt,
                Status = SessionStatus.InProgress,
                JitsiRoomId = $"edskill-{sessionId:N}",
                ActualStartAt = scheduledAt.AddMinutes(1),
                ActualDuration = 14,
                UpdatedAt = updatedAt
            });
            seedContext.SessionPresenceSegments.AddRange(
                new SessionPresenceSegment
                {
                    SessionPresenceSegmentId = Guid.NewGuid(),
                    SessionId = sessionId,
                    UserId = companionId,
                    JoinedAt = scheduledAt.AddMinutes(1)
                },
                new SessionPresenceSegment
                {
                    SessionPresenceSegmentId = Guid.NewGuid(),
                    SessionId = sessionId,
                    UserId = learnerId,
                    JoinedAt = scheduledAt.AddMinutes(2)
                });

            await seedContext.SaveChangesAsync();
        }

        await using var configContext = new AppDbContext(options);
        var builder = new SessionRealtimeSnapshotBuilder(
            new TestDbContextFactory(options),
            new SystemConfigService(configContext));

        var snapshot = await builder.BuildRoomStateSnapshotAsync(sessionId, CancellationToken.None);

        snapshot.Should().NotBeNull();
        snapshot!.Payload.SessionId.Should().Be(sessionId);
        snapshot.Payload.Status.Should().Be(SessionStatus.InProgress);
        snapshot.Payload.JitsiRoomId.Should().Be($"edskill-{sessionId:N}");
        snapshot.Payload.HasCompanionJoined.Should().BeTrue();
        snapshot.Payload.HasLearnerJoined.Should().BeTrue();
        snapshot.Payload.ActiveParticipantCount.Should().Be(2);
        snapshot.Payload.ActualDuration.Should().Be(14);
        snapshot.Payload.JoinOpenAt.Should().Be(scheduledAt.AddMinutes(-10));
        snapshot.Payload.JoinCloseAt.Should().Be(scheduledAt.AddMinutes(90));
        snapshot.Payload.UpdatedAt.Should().Be(updatedAt);
        snapshot.UserGroups.Should().Contain(SessionRealtimeGroupNames.User(companionId));
        snapshot.UserGroups.Should().Contain(SessionRealtimeGroupNames.User(learnerId));

        await using var cleanupContext = new AppDbContext(options);
        await cleanupContext.Database.EnsureDeletedAsync();
    }

    private static DbContextOptions<AppDbContext> CreateOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    private static User CreateUser(Guid userId, string username, string role)
    {
        return new User
        {
            UserId = userId,
            Username = username,
            Email = $"{username}@test.local",
            PasswordHash = "hash",
            Roles = [role],
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                DisplayName = username
            }
        };
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(_options);
        }

        public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AppDbContext(_options));
        }
    }
}
