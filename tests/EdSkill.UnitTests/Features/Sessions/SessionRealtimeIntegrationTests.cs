using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using EdSkill.API.Realtime;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdSkill.UnitTests.Features.Sessions;

public class SessionRealtimeIntegrationTests
{
    [Fact]
    public async Task ConnectAndSubscribe_WhenUnauthenticatedOrNonParticipant_IsRejected()
    {
        await using var factory = new SessionRealtimeWebApplicationFactory();
        var sessionId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();

        await factory.ResetDatabaseAsync(db =>
        {
            db.Users.AddRange(
                CreateUser(companionId, "companion-1", "companion"),
                CreateUser(learnerId, "learner-1", "learner"),
                CreateUser(intruderId, "intruder-1", "learner"));
            db.Sessions.Add(new Session
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Realtime",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddHours(1),
                Status = SessionStatus.Confirmed,
                JitsiRoomId = $"edskill-{sessionId:N}"
            });
        });

        var unauthenticatedConnection = CreateHubConnection(factory, null);
        var intruderConnection = CreateHubConnection(factory, CreateTestAuthorizationValue(intruderId, "learner"));

        await Assert.ThrowsAnyAsync<Exception>(() => unauthenticatedConnection.StartAsync());

        await intruderConnection.StartAsync();
        var subscribeAction = async () => await intruderConnection.InvokeAsync("SubscribeSession", sessionId);
        await Assert.ThrowsAsync<HubException>(subscribeAction);

        await intruderConnection.DisposeAsync();
        await unauthenticatedConnection.DisposeAsync();
    }

    [Fact]
    public async Task JoinSession_WhenCompanionJoins_PublishesRoomStateToSubscribedLearner()
    {
        await using var factory = new SessionRealtimeWebApplicationFactory();
        var sessionId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();

        await factory.ResetDatabaseAsync(db =>
        {
            db.Users.AddRange(
                CreateUser(companionId, "companion-join", "companion"),
                CreateUser(learnerId, "learner-join", "learner"));
            db.Sessions.Add(new Session
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Realtime",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddMinutes(5),
                Status = SessionStatus.Confirmed,
                JitsiRoomId = $"edskill-{sessionId:N}"
            });
        });

        var learnerConnection = CreateHubConnection(factory, CreateTestAuthorizationValue(learnerId, "learner"));
        var roomStateTcs = new TaskCompletionSource<SessionRoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        learnerConnection.On<SessionRoomStateDto>(
            SessionRealtimeEventNames.SessionRoomStateUpdated,
            payload => roomStateTcs.TrySetResult(payload));

        await learnerConnection.StartAsync();
        await learnerConnection.InvokeAsync("SubscribeSession", sessionId);

        using var companionClient = CreateAuthorizedClient(factory, companionId, "companion");
        var response = await companionClient.PostAsync($"/api/sessions/{sessionId}/join", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roomState = await WaitForEventAsync(roomStateTcs.Task);
        roomState.SessionId.Should().Be(sessionId);
        roomState.HasCompanionJoined.Should().BeTrue();
        roomState.ActiveParticipantCount.Should().Be(1);
        roomState.Status.Should().Be(SessionStatus.InProgress);

        await learnerConnection.DisposeAsync();
    }

    [Fact]
    public async Task LeaveSession_WhenLastParticipantLeaves_PublishesPendingReviewToUserAndSessionGroups()
    {
        await using var factory = new SessionRealtimeWebApplicationFactory();
        var sessionId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddMinutes(-30);

        await factory.ResetDatabaseAsync(db =>
        {
            db.Users.AddRange(
                CreateUser(companionId, "companion-leave", "companion"),
                CreateUser(learnerId, "learner-leave", "learner"));
            db.Sessions.Add(new Session
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
                ActualStartAt = scheduledAt
            });
            db.SessionPresenceSegments.AddRange(
                new SessionPresenceSegment
                {
                    SessionPresenceSegmentId = Guid.NewGuid(),
                    SessionId = sessionId,
                    UserId = learnerId,
                    JoinedAt = scheduledAt,
                    LeftAt = scheduledAt.AddMinutes(12)
                },
                new SessionPresenceSegment
                {
                    SessionPresenceSegmentId = Guid.NewGuid(),
                    SessionId = sessionId,
                    UserId = companionId,
                    JoinedAt = scheduledAt
                });
        });

        var learnerConnection = CreateHubConnection(factory, CreateTestAuthorizationValue(learnerId, "learner"));
        var companionConnection = CreateHubConnection(factory, CreateTestAuthorizationValue(companionId, "companion"));
        var learnerRoomStateTcs = new TaskCompletionSource<SessionRoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var companionRoomStateTcs = new TaskCompletionSource<SessionRoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        learnerConnection.On<SessionRoomStateDto>(
            SessionRealtimeEventNames.SessionRoomStateUpdated,
            payload => learnerRoomStateTcs.TrySetResult(payload));
        companionConnection.On<SessionRoomStateDto>(
            SessionRealtimeEventNames.SessionRoomStateUpdated,
            payload => companionRoomStateTcs.TrySetResult(payload));

        await learnerConnection.StartAsync();
        await companionConnection.StartAsync();
        await learnerConnection.InvokeAsync("SubscribeSession", sessionId);

        using var companionClient = CreateAuthorizedClient(factory, companionId, "companion");
        var response = await companionClient.PostAsJsonAsync($"/api/sessions/{sessionId}/leave", new LeaveSessionRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var learnerRoomState = await WaitForEventAsync(learnerRoomStateTcs.Task);
        var companionRoomState = await WaitForEventAsync(companionRoomStateTcs.Task);
        learnerRoomState.SessionId.Should().Be(sessionId);
        companionRoomState.SessionId.Should().Be(sessionId);
        learnerRoomState.Should().BeEquivalentTo(companionRoomState);

        await learnerConnection.DisposeAsync();
        await companionConnection.DisposeAsync();
    }

    [Fact]
    public async Task ConfirmCompletion_WhenBothParticipantsConfirm_PublishesUpdatedAndCompletedSnapshots()
    {
        await using var factory = new SessionRealtimeWebApplicationFactory();
        var sessionId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();

        await factory.ResetDatabaseAsync(db =>
        {
            db.Users.AddRange(
                CreateUser(companionId, "companion-complete", "companion"),
                CreateUser(learnerId, "learner-complete", "learner"));
            db.PointWallets.Add(new PointWallet
            {
                PointWalletId = Guid.NewGuid(),
                UserId = learnerId,
                Balance = 0,
                HeldBalance = 100
            });
            db.Sessions.Add(new Session
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Realtime",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddHours(-2),
                Status = SessionStatus.PendingReview,
                JitsiRoomId = $"edskill-{sessionId:N}",
                ActualStartAt = DateTime.UtcNow.AddHours(-2),
                ActualEndAt = DateTime.UtcNow.AddHours(-1),
                ActualDuration = 30
            });
        });

        var learnerConnection = CreateHubConnection(factory, CreateTestAuthorizationValue(learnerId, "learner"));
        var firstUpdateTcs = new TaskCompletionSource<SessionDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondUpdateTcs = new TaskCompletionSource<SessionDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var updateCount = 0;

        learnerConnection.On<SessionDto>(
            SessionRealtimeEventNames.SessionUpdated,
            payload =>
            {
                updateCount++;
                if (updateCount == 1)
                {
                    firstUpdateTcs.TrySetResult(payload);
                }
                else if (updateCount == 2)
                {
                    secondUpdateTcs.TrySetResult(payload);
                }
            });

        await learnerConnection.StartAsync();

        using var learnerClient = CreateAuthorizedClient(factory, learnerId, "learner");
        using var companionClient = CreateAuthorizedClient(factory, companionId, "companion");

        var learnerResponse = await learnerClient.PostAsync($"/api/sessions/{sessionId}/confirm-completion", null);
        learnerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstUpdate = await WaitForEventAsync(firstUpdateTcs.Task);
        firstUpdate.Status.Should().Be(SessionStatus.PendingReview);
        firstUpdate.LearnerConfirmed.Should().BeTrue();
        firstUpdate.CompanionConfirmed.Should().BeFalse();

        var companionResponse = await companionClient.PostAsync($"/api/sessions/{sessionId}/confirm-completion", null);
        companionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondUpdate = await WaitForEventAsync(secondUpdateTcs.Task);
        secondUpdate.Status.Should().Be(SessionStatus.Completed);
        secondUpdate.LearnerConfirmed.Should().BeTrue();
        secondUpdate.CompanionConfirmed.Should().BeTrue();

        await learnerConnection.DisposeAsync();
    }

    [Theory]
    [InlineData("book")]
    [InlineData("confirm")]
    [InlineData("reject")]
    [InlineData("cancel")]
    public async Task SessionMutations_WhenStateChanges_PublishSessionUpdatedSnapshots(string scenario)
    {
        await using var factory = new SessionRealtimeWebApplicationFactory();
        var sessionId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();

        await factory.ResetDatabaseAsync(db => SeedScenario(db, scenario, sessionId, companionId, learnerId));

        var listenerUserId = scenario switch
        {
            "book" => companionId,
            "confirm" => learnerId,
            "reject" => learnerId,
            "cancel" => companionId,
            _ => throw new InvalidOperationException("Unsupported scenario.")
        };

        var listenerRole = scenario switch
        {
            "book" => "companion",
            "confirm" => "learner",
            "reject" => "learner",
            "cancel" => "companion",
            _ => throw new InvalidOperationException("Unsupported scenario.")
        };

        var actorUserId = scenario switch
        {
            "book" => learnerId,
            "confirm" => companionId,
            "reject" => companionId,
            "cancel" => learnerId,
            _ => throw new InvalidOperationException("Unsupported scenario.")
        };

        var actorRole = scenario switch
        {
            "book" => "learner",
            "confirm" => "companion",
            "reject" => "companion",
            "cancel" => "learner",
            _ => throw new InvalidOperationException("Unsupported scenario.")
        };

        var listenerConnection = CreateHubConnection(factory, CreateTestAuthorizationValue(listenerUserId, listenerRole));
        var updateTcs = new TaskCompletionSource<SessionDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        listenerConnection.On<SessionDto>(
            SessionRealtimeEventNames.SessionUpdated,
            payload => updateTcs.TrySetResult(payload));
        await listenerConnection.StartAsync();

        using var actorClient = CreateAuthorizedClient(factory, actorUserId, actorRole);
        var response = await ExecuteScenarioAsync(actorClient, scenario, sessionId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var update = await WaitForEventAsync(updateTcs.Task);
        update.SessionId.Should().Be(sessionId);
        update.Status.Should().Be(ExpectedStatusForScenario(scenario));

        await listenerConnection.DisposeAsync();
    }

    [Fact]
    public async Task MutationFails_WhenCommandRejected_DoesNotPublishRealtimeEvents()
    {
        await using var factory = new SessionRealtimeWebApplicationFactory();
        var sessionId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();

        await factory.ResetDatabaseAsync(db =>
        {
            db.Users.AddRange(
                CreateUser(companionId, "companion-fail", "companion"),
                CreateUser(learnerId, "learner-fail", "learner"));
            db.Sessions.Add(new Session
            {
                SessionId = sessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                Skill = "Realtime",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddHours(1),
                Status = SessionStatus.Pending
            });
        });

        var companionConnection = CreateHubConnection(factory, CreateTestAuthorizationValue(companionId, "companion"));
        var sessionUpdatedTcs = new TaskCompletionSource<SessionDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var roomStateTcs = new TaskCompletionSource<SessionRoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        companionConnection.On<SessionDto>(
            SessionRealtimeEventNames.SessionUpdated,
            payload => sessionUpdatedTcs.TrySetResult(payload));
        companionConnection.On<SessionRoomStateDto>(
            SessionRealtimeEventNames.SessionRoomStateUpdated,
            payload => roomStateTcs.TrySetResult(payload));
        await companionConnection.StartAsync();

        using var learnerClient = CreateAuthorizedClient(factory, learnerId, "learner");
        var response = await learnerClient.PostAsync($"/api/sessions/{sessionId}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await DidReceiveEventAsync(sessionUpdatedTcs.Task, 800)).Should().BeFalse();
        (await DidReceiveEventAsync(roomStateTcs.Task, 800)).Should().BeFalse();

        await companionConnection.DisposeAsync();
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

    private static PointWallet CreateWallet(Guid userId, int balance, int heldBalance)
    {
        return new PointWallet
        {
            PointWalletId = Guid.NewGuid(),
            UserId = userId,
            Balance = balance,
            HeldBalance = heldBalance
        };
    }

    private static HubConnection CreateHubConnection(SessionRealtimeWebApplicationFactory factory, string? accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl(
                "https://localhost/hubs/sessions",
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                    if (!string.IsNullOrWhiteSpace(accessToken))
                    {
                        options.Headers["Authorization"] = accessToken;
                    }
                })
            .Build();
    }

    private static HttpClient CreateAuthorizedClient(SessionRealtimeWebApplicationFactory factory, Guid userId, string role)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CreateTestAuthorizationValue(userId, role));
        return client;
    }

    private static string CreateTestAuthorizationValue(Guid userId, string role)
    {
        return $"Test {userId:D}|{role}";
    }

    private static async Task<T> WaitForEventAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
        completed.Should().Be(task, "expected SignalR event to arrive before timeout");
        return await task;
    }

    private static async Task<bool> DidReceiveEventAsync<T>(Task<T> task, int timeoutMs)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
        return completed == task;
    }

    private static SessionStatus ExpectedStatusForScenario(string scenario)
    {
        return scenario switch
        {
            "book" => SessionStatus.Pending,
            "confirm" => SessionStatus.Confirmed,
            "reject" => SessionStatus.Cancelled,
            "cancel" => SessionStatus.Cancelled,
            _ => throw new InvalidOperationException("Unsupported scenario.")
        };
    }

    private static async Task<HttpResponseMessage> ExecuteScenarioAsync(HttpClient client, string scenario, Guid sessionId)
    {
        return scenario switch
        {
            "book" => await client.PostAsJsonAsync($"/api/sessions/{sessionId}/book", new BookSessionRequest(60)),
            "confirm" => await client.PostAsync($"/api/sessions/{sessionId}/confirm", null),
            "reject" => await client.PostAsJsonAsync($"/api/sessions/{sessionId}/reject", new RejectSessionRequest("Busy")),
            "cancel" => await client.PostAsJsonAsync($"/api/sessions/{sessionId}/cancel", new CancelSessionRequest("Need to reschedule")),
            _ => throw new InvalidOperationException("Unsupported scenario.")
        };
    }

    private static void SeedScenario(AppDbContext db, string scenario, Guid sessionId, Guid companionId, Guid learnerId)
    {
        db.Users.AddRange(
            CreateUser(companionId, $"companion-{scenario}", "companion"),
            CreateUser(learnerId, $"learner-{scenario}", "learner"));

        switch (scenario)
        {
            case "book":
                db.PointWallets.Add(CreateWallet(learnerId, 500, 0));
                db.Sessions.Add(new Session
                {
                    SessionId = sessionId,
                    CompanionId = companionId,
                    Skill = "Realtime",
                    DeliveryMode = SessionDeliveryMode.Online,
                    DurationMinutes = 60,
                    PointCost = 100,
                    ScheduledAt = DateTime.UtcNow.AddHours(1),
                    Status = SessionStatus.Available
                });
                break;

            case "confirm":
                db.Sessions.Add(new Session
                {
                    SessionId = sessionId,
                    CompanionId = companionId,
                    LearnerId = learnerId,
                    Skill = "Realtime",
                    DeliveryMode = SessionDeliveryMode.Online,
                    DurationMinutes = 60,
                    PointCost = 100,
                    ScheduledAt = DateTime.UtcNow.AddHours(1),
                    Status = SessionStatus.Pending
                });
                break;

            case "reject":
                db.PointWallets.Add(CreateWallet(learnerId, 0, 100));
                db.Sessions.Add(new Session
                {
                    SessionId = sessionId,
                    CompanionId = companionId,
                    LearnerId = learnerId,
                    Skill = "Realtime",
                    DeliveryMode = SessionDeliveryMode.Online,
                    DurationMinutes = 60,
                    PointCost = 100,
                    ScheduledAt = DateTime.UtcNow.AddHours(1),
                    Status = SessionStatus.Pending
                });
                break;

            case "cancel":
                db.PointWallets.Add(CreateWallet(learnerId, 0, 100));
                db.Sessions.Add(new Session
                {
                    SessionId = sessionId,
                    CompanionId = companionId,
                    LearnerId = learnerId,
                    Skill = "Realtime",
                    DeliveryMode = SessionDeliveryMode.Online,
                    DurationMinutes = 60,
                    PointCost = 100,
                    ScheduledAt = DateTime.UtcNow.AddHours(1),
                    Status = SessionStatus.Pending
                });
                break;

            default:
                throw new InvalidOperationException("Unsupported scenario.");
        }
    }

    private sealed class SessionRealtimeWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        public const string JwtSecret = "edskill-test-secret-edskill-test-secret-1234";
        public const string JwtIssuer = "EdSkill.Tests";
        public const string JwtAudience = "EdSkill.Tests.Client";

        private readonly string _databaseName = $"EdSkillSignalR_{Guid.NewGuid():N}";

        private string ConnectionString =>
            $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:MyCnn"] = ConnectionString,
                    ["JwtSettings:SecretKey"] = JwtSecret,
                    ["JwtSettings:Issuer"] = JwtIssuer,
                    ["JwtSettings:Audience"] = JwtAudience,
                    ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                    ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                    ["JwtSettings:ResetPasswordTokenExpirationMinutes"] = "15",
                    ["CorsSettings:AllowedOrigins:0"] = "https://localhost",
                    ["EmailSettings:ApiKey"] = "test",
                    ["EmailSettings:ResendApiKey"] = "test",
                    ["EmailSettings:SenderEmail"] = "test@local",
                    ["EmailSettings:SenderName"] = "EdSkill Test",
                    ["GoogleAuth:ClientId"] = "test-client-id",
                    ["R2Storage:AccountId"] = "test",
                    ["R2Storage:AccessKeyId"] = "test",
                    ["R2Storage:SecretAccessKey"] = "test",
                    ["R2Storage:BucketName"] = "test",
                    ["R2Storage:PublicBaseUrl"] = "https://localhost/storage",
                    ["VnPaySettings:TerminalCode"] = "test",
                    ["VnPaySettings:HashSecret"] = "test"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging => logging.ClearProviders());
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.RemoveAll<AppDbContext>();
                services.RemoveAll<IApplicationDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextFactory<AppDbContext>>();

                services.RemoveAll<ITransactionExecutor>();

                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
                services.AddDbContextFactory<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName), ServiceLifetime.Scoped);
                services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());
                services.AddScoped<ITransactionExecutor, TestTransactionExecutor>();
            });
        }

        public async Task ResetDatabaseAsync(Action<AppDbContext> seed)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            seed(dbContext);
            await dbContext.SaveChangesAsync();
        }

        public override async ValueTask DisposeAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await base.DisposeAsync();
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorization = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Test ", StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var payload = authorization["Test ".Length..];
            var parts = payload.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test authorization header."));
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(ClaimTypes.Name, $"user-{userId:N}")
            };

            foreach (var role in parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class TestTransactionExecutor : ITransactionExecutor
    {
        private readonly AppDbContext _dbContext;

        public TestTransactionExecutor(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EdSkill.Application.Common.Models.Result> ExecuteAsync(
            Func<CancellationToken, Task<EdSkill.Application.Common.Models.Result>> operation,
            CancellationToken cancellationToken = default)
        {
            var result = await operation(cancellationToken);
            if (!result.IsSuccess)
            {
                return result;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return result;
        }

        public async Task<EdSkill.Application.Common.Models.Result<T>> ExecuteAsync<T>(
            Func<CancellationToken, Task<EdSkill.Application.Common.Models.Result<T>>> operation,
            CancellationToken cancellationToken = default)
        {
            var result = await operation(cancellationToken);
            if (!result.IsSuccess)
            {
                return result;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

}
