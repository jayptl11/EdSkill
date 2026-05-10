using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Auth.Commands.VerifyOtp;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;

namespace EdSkill.UnitTests.Features.Auth;

public class VerifyOtpCommandHandlerTests
{
    private const string RegistrationData = """
        {"Username":"testuser","PasswordHash":"hashedPassword","FirstName":"John","LastName":"Doe","SignupIntent":"teach","Roles":["learner","companion"],"AcceptedPolicies":[{"PolicyType":"terms","PolicyVersion":"2026-05-10.v1"},{"PolicyType":"privacy","PolicyVersion":"2026-05-10.v1"},{"PolicyType":"points_tokens","PolicyVersion":"2026-05-10.v1"}]}
        """;

    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IOTPCacheService> _otpCacheServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IPolicyConsentService> _policyConsentServiceMock;
    private readonly Mock<ISystemConfigService> _systemConfigServiceMock;
    private readonly Mock<IPointLedgerService> _pointLedgerServiceMock;
    private readonly VerifyOtpCommandHandler _handler;

    public VerifyOtpCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _otpCacheServiceMock = new Mock<IOTPCacheService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _policyConsentServiceMock = new Mock<IPolicyConsentService>();
        _systemConfigServiceMock = new Mock<ISystemConfigService>();
        _pointLedgerServiceMock = new Mock<IPointLedgerService>();
        _tokenServiceMock.Setup(x => x.HashRefreshToken(It.IsAny<string>())).Returns((string s) => s);
        _handler = new VerifyOtpCommandHandler(
            _contextMock.Object,
            _otpCacheServiceMock.Object,
            _tokenServiceMock.Object,
            _policyConsentServiceMock.Object,
            _systemConfigServiceMock.Object,
            _pointLedgerServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoOtpFound_ReturnsFailure()
    {
        var command = new VerifyOtpCommand("notfound@test.com", "123456");
        _otpCacheServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string, OtpPurpose)>.Failure("INVALID_OTP", "No pending verification found for this email"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_OTP");
    }

    [Fact]
    public async Task Handle_WhenOtpInvalid_ReturnsFailure()
    {
        var command = new VerifyOtpCommand("test@test.com", "123456");
        _otpCacheServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string, OtpPurpose)>.Failure("INVALID_OTP", "Invalid OTP code"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_OTP");
    }

    [Fact]
    public async Task Handle_WhenValidRegisterOtp_CreatesUserProfileAndPolicyConsents()
    {
        var command = new VerifyOtpCommand("test@test.com", "123456");
        var users = new List<User>();
        var profiles = new List<UserProfile>();
        var policyConsents = new List<PolicyConsent>();

        _otpCacheServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string, OtpPurpose)>.Success((RegistrationData, OtpPurpose.Register)));
        _otpCacheServiceMock.Setup(x => x.DeleteOtpDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _policyConsentServiceMock
            .Setup(service => service.BuildRegistrationPolicyConsentsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<PolicyAcceptanceInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, IReadOnlyCollection<PolicyAcceptanceInput>? _, CancellationToken _) =>
                Result<IReadOnlyCollection<PolicyConsent>>.Success(
                [
                    new PolicyConsent { PolicyConsentId = Guid.NewGuid(), UserId = userId, PolicyType = PolicyType.Terms, PolicyVersion = "2026-05-10.v1", AcceptedAt = DateTime.UtcNow },
                    new PolicyConsent { PolicyConsentId = Guid.NewGuid(), UserId = userId, PolicyType = PolicyType.Privacy, PolicyVersion = "2026-05-10.v1", AcceptedAt = DateTime.UtcNow },
                    new PolicyConsent { PolicyConsentId = Guid.NewGuid(), UserId = userId, PolicyType = PolicyType.PointsTokens, PolicyVersion = "2026-05-10.v1", AcceptedAt = DateTime.UtcNow }
                ]));

        _systemConfigServiceMock
            .Setup(service => service.GetIntValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        _pointLedgerServiceMock
            .Setup(service => service.GetOrCreateWalletAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, CancellationToken _) => new PointWallet { PointWalletId = Guid.NewGuid(), UserId = userId });

        _pointLedgerServiceMock
            .Setup(service => service.ApplySignupBonusAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        SetupUsersDbSet(users);
        SetupUserProfilesDbSet(profiles);
        SetupPolicyConsentsDbSet(policyConsents);

        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Purpose.Should().Be(OtpPurpose.Register);
        users.Should().HaveCount(1);
        users[0].Roles.Should().BeEquivalentTo("learner", "companion");
        profiles.Should().HaveCount(1);
        policyConsents.Should().HaveCount(3);
        policyConsents.Should().OnlyContain(consent => consent.UserId == users[0].UserId);
        _pointLedgerServiceMock.Verify(service => service.ApplySignupBonusAsync(users[0].UserId, 50, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ReturnsFailure()
    {
        var command = new VerifyOtpCommand("test@test.com", "123456");
        _otpCacheServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string, OtpPurpose)>.Success((RegistrationData, OtpPurpose.Register)));

        SetupUsersDbSet([new User { Email = "test@test.com", Username = "testuser" }]);
        SetupUserProfilesDbSet([]);
        SetupPolicyConsentsDbSet([]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_EXISTS");
    }

    private void SetupUsersDbSet(List<User> users)
    {
        var queryable = new TestAsyncEnumerable<User>(users);
        var dbSetMock = new Mock<DbSet<User>>();
        dbSetMock.As<IQueryable<User>>().Setup(m => m.Provider).Returns(queryable.AsQueryable().Provider);
        dbSetMock.As<IQueryable<User>>().Setup(m => m.Expression).Returns(queryable.AsQueryable().Expression);
        dbSetMock.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(queryable.AsQueryable().ElementType);
        dbSetMock.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(queryable.AsQueryable().GetEnumerator());
        dbSetMock.As<IAsyncEnumerable<User>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(queryable.GetAsyncEnumerator());
        dbSetMock.Setup(x => x.Add(It.IsAny<User>())).Callback<User>(users.Add);
        dbSetMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => users.Add(user))
            .Returns(new ValueTask<EntityEntry<User>>((EntityEntry<User>)null!));
        _contextMock.Setup(x => x.Users).Returns(dbSetMock.Object);
    }

    private void SetupUserProfilesDbSet(List<UserProfile> profiles)
    {
        var queryable = new TestAsyncEnumerable<UserProfile>(profiles);
        var dbSetMock = new Mock<DbSet<UserProfile>>();
        dbSetMock.As<IQueryable<UserProfile>>().Setup(m => m.Provider).Returns(queryable.AsQueryable().Provider);
        dbSetMock.As<IQueryable<UserProfile>>().Setup(m => m.Expression).Returns(queryable.AsQueryable().Expression);
        dbSetMock.As<IQueryable<UserProfile>>().Setup(m => m.ElementType).Returns(queryable.AsQueryable().ElementType);
        dbSetMock.As<IQueryable<UserProfile>>().Setup(m => m.GetEnumerator()).Returns(queryable.AsQueryable().GetEnumerator());
        dbSetMock.As<IAsyncEnumerable<UserProfile>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(queryable.GetAsyncEnumerator());
        dbSetMock.Setup(x => x.Add(It.IsAny<UserProfile>())).Callback<UserProfile>(profiles.Add);
        dbSetMock.Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((profile, _) => profiles.Add(profile))
            .Returns(new ValueTask<EntityEntry<UserProfile>>((EntityEntry<UserProfile>)null!));
        _contextMock.Setup(x => x.UserProfiles).Returns(dbSetMock.Object);
    }

    private void SetupPolicyConsentsDbSet(List<PolicyConsent> policyConsents)
    {
        var queryable = new TestAsyncEnumerable<PolicyConsent>(policyConsents);
        var dbSetMock = new Mock<DbSet<PolicyConsent>>();
        dbSetMock.As<IQueryable<PolicyConsent>>().Setup(m => m.Provider).Returns(queryable.AsQueryable().Provider);
        dbSetMock.As<IQueryable<PolicyConsent>>().Setup(m => m.Expression).Returns(queryable.AsQueryable().Expression);
        dbSetMock.As<IQueryable<PolicyConsent>>().Setup(m => m.ElementType).Returns(queryable.AsQueryable().ElementType);
        dbSetMock.As<IQueryable<PolicyConsent>>().Setup(m => m.GetEnumerator()).Returns(queryable.AsQueryable().GetEnumerator());
        dbSetMock.As<IAsyncEnumerable<PolicyConsent>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(queryable.GetAsyncEnumerator());
        dbSetMock.Setup(x => x.AddRange(It.IsAny<IEnumerable<PolicyConsent>>()))
            .Callback<IEnumerable<PolicyConsent>>(consents =>
            {
                foreach (var consent in consents)
                {
                    policyConsents.Add(consent);
                }
            });
        _contextMock.Setup(x => x.PolicyConsents).Returns(dbSetMock.Object);
    }
}
