using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Auth;
using EdSkill.Application.Features.Auth.Commands.Register;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EdSkill.UnitTests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private static readonly PolicyAcceptanceInput[] AcceptedPolicies =
    [
        new("terms", "2026-05-10.v1"),
        new("privacy", "2026-05-10.v1"),
        new("points_tokens", "2026-05-10.v1")
    ];

    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IOTPCacheService> _otpCacheServiceMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<IPolicyConsentService> _policyConsentServiceMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _emailServiceMock = new Mock<IEmailService>();
        _otpCacheServiceMock = new Mock<IOTPCacheService>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _policyConsentServiceMock = new Mock<IPolicyConsentService>();
        _handler = new RegisterCommandHandler(
            _contextMock.Object,
            _emailServiceMock.Object,
            _otpCacheServiceMock.Object,
            _passwordServiceMock.Object,
            _policyConsentServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsFailure()
    {
        var command = new RegisterCommand("existing@test.com", "newuser", "John", "Doe", "Password123", SignupIntents.Learn, AcceptedPolicies);
        SetupUsersDbSet([new User { Email = "existing@test.com", Username = "existinguser" }]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EMAIL_EXISTS");
    }

    [Fact]
    public async Task Handle_WhenUsernameAlreadyExists_ReturnsFailure()
    {
        var command = new RegisterCommand("new@test.com", "existinguser", "John", "Doe", "Password123", SignupIntents.Learn, AcceptedPolicies);
        SetupUsersDbSet([new User { Email = "other@test.com", Username = "existinguser" }]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("USERNAME_EXISTS");
    }

    [Fact]
    public async Task Handle_WhenPolicyVersionIsStale_ReturnsFailure()
    {
        var command = new RegisterCommand("new@test.com", "newuser", "John", "Doe", "Password123", SignupIntents.Learn, AcceptedPolicies);
        SetupUsersDbSet([]);
        _policyConsentServiceMock
            .Setup(service => service.ValidateRegistrationPolicyAcceptancesAsync(It.IsAny<IReadOnlyCollection<PolicyAcceptanceInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("POLICY_VERSION_INVALID", "Policy version is not the active version."));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("POLICY_VERSION_INVALID");
    }

    [Fact]
    public async Task Handle_WhenValidRequest_CreatesOtpAndSendsEmail()
    {
        var command = new RegisterCommand("new@test.com", "newuser", "John", "Doe", "Password123", SignupIntents.Teach, AcceptedPolicies);
        SetupUsersDbSet([]);

        _policyConsentServiceMock
            .Setup(service => service.ValidateRegistrationPolicyAcceptancesAsync(It.IsAny<IReadOnlyCollection<PolicyAcceptanceInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _passwordServiceMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashedPassword");
        _otpCacheServiceMock.Setup(x => x.GenerateAndStoreOtpAsync(It.IsAny<string>(), It.IsAny<OtpPurpose>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _otpCacheServiceMock.Setup(x => x.GetLastGeneratedOtp()).Returns("123456");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _otpCacheServiceMock.Verify(x => x.GenerateAndStoreOtpAsync(
            "new@test.com",
            OtpPurpose.Register,
            It.Is<string>(payload =>
                payload.Contains("\"SignupIntent\":\"teach\"") &&
                payload.Contains("\"Roles\":[\"learner\",\"companion\"]") &&
                payload.Contains("\"PolicyType\":\"terms\"") &&
                payload.Contains("\"PolicyVersion\":\"2026-05-10.v1\"")),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendOtpEmailAsync("new@test.com", "123456", It.IsAny<CancellationToken>()), Times.Once);
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
        _contextMock.Setup(x => x.Users).Returns(dbSetMock.Object);
    }
}
