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
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IOTPCacheService> _otpCacheServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly VerifyOtpCommandHandler _handler;

    public VerifyOtpCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _otpCacheServiceMock = new Mock<IOTPCacheService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _tokenServiceMock.Setup(x => x.HashRefreshToken(It.IsAny<string>())).Returns((string s) => s);
        _handler = new VerifyOtpCommandHandler(
            _contextMock.Object,
            _otpCacheServiceMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoOtpFound_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyOtpCommand("notfound@test.com", "123456");
        _otpCacheServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string, OtpPurpose)>.Failure("INVALID_OTP", "No pending verification found for this email"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_OTP");
    }

    [Fact]
    public async Task Handle_WhenOtpInvalid_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyOtpCommand("test@test.com", "123456");
        _otpCacheServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string, OtpPurpose)>.Failure("INVALID_OTP", "Invalid OTP code"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_OTP");
    }

    [Fact]
    public async Task Handle_WhenValidRegisterOtp_CreatesUserAndReturnsSuccess()
    {
        // Arrange
        var command = new VerifyOtpCommand("test@test.com", "123456");
        var registrationData = "{\"Username\":\"testuser\",\"PasswordHash\":\"hashedPassword\",\"FirstName\":\"John\",\"LastName\":\"Doe\",\"Roles\":[\"learner\",\"companion\"]}";
        var users = new List<User>();
        var profiles = new List<UserProfile>();
        
        _otpCacheServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string, OtpPurpose)>.Success((registrationData, OtpPurpose.Register)));
        _otpCacheServiceMock.Setup(x => x.DeleteOtpDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        SetupUsersDbSet(users);
        SetupUserProfilesDbSet(profiles);
        
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Purpose.Should().Be(OtpPurpose.Register);
        result.Value.Message.Should().Be("Registration successful");
        users.Should().HaveCount(1);
        profiles.Should().HaveCount(1);
        users[0].Roles.Should().BeEquivalentTo("learner", "companion");
        users[0].Status.Should().Be("active");
        profiles[0].DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyOtpCommand("test@test.com", "123456");
        var registrationData = "{\"Username\":\"testuser\",\"PasswordHash\":\"hashedPassword\",\"FirstName\":\"John\",\"LastName\":\"Doe\",\"Roles\":[\"learner\"]}";
        var existingUser = new User { Email = "test@test.com", Username = "testuser" };
        
        _otpCacheServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string, OtpPurpose)>.Success((registrationData, OtpPurpose.Register)));
        
        SetupUsersDbSet(new List<User> { existingUser });
        SetupUserProfilesDbSet(new List<UserProfile>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
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
}
