using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Auth.Commands.Register;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Auth.Validators;

public class RegisterCommandValidatorTests
{
    private static readonly PolicyAcceptanceInput[] ValidAcceptedPolicies =
    [
        new("terms", "2026-05-10.v1"),
        new("privacy", "2026-05-10.v1"),
        new("points_tokens", "2026-05-10.v1")
    ];

    private readonly RegisterCommandValidator _validator;

    public RegisterCommandValidatorTests()
    {
        _validator = new RegisterCommandValidator();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Email_WhenEmpty_ShouldHaveError(string? email)
    {
        var command = BuildCommand(email: email!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@invalid.com")]
    [InlineData("invalid.com")]
    public void Email_WhenInvalidFormat_ShouldHaveError(string email)
    {
        var command = BuildCommand(email: email);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorCode("INVALID_EMAIL_FORMAT");
    }

    [Fact]
    public void Email_WhenValid_ShouldNotHaveError()
    {
        var command = BuildCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Username_WhenEmpty_ShouldHaveError(string? username)
    {
        var command = BuildCommand(username: username!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void Username_WhenTooShort_ShouldHaveError(string username)
    {
        var command = BuildCommand(username: username);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorCode("INVALID_USERNAME");
    }

    [Fact]
    public void Username_WhenTooLong_ShouldHaveError()
    {
        var command = BuildCommand(username: new string('a', 51));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorCode("INVALID_USERNAME");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("validuser")]
    [InlineData("user123")]
    public void Username_WhenValid_ShouldNotHaveError(string username)
    {
        var command = BuildCommand(username: username);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Password_WhenEmpty_ShouldHaveError(string? password)
    {
        var command = BuildCommand(password: password!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("nouppercase1")]
    [InlineData("NOLOWERCASE1")]
    [InlineData("NoNumbers")]
    [InlineData("Pass1")]
    public void Password_WhenInvalidFormat_ShouldHaveError(string password)
    {
        var command = BuildCommand(password: password);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("INVALID_PASSWORD");
    }

    [Theory]
    [InlineData("Password1")]
    [InlineData("ValidPass123")]
    [InlineData("MyP@ssw0rd")]
    public void Password_WhenValid_ShouldNotHaveError(string password)
    {
        var command = BuildCommand(password: password);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("learner")]
    [InlineData("companion")]
    public void Roles_WhenSingleAllowedRole_ShouldNotHaveError(string role)
    {
        var command = BuildCommand(roles: [role]);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Roles);
    }

    [Fact]
    public void Roles_WhenLearnerAndCompanion_ShouldNotHaveError()
    {
        var command = BuildCommand(roles: ["learner", "companion"]);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Roles);
    }

    [Fact]
    public void Roles_WhenEmpty_ShouldHaveError()
    {
        var command = BuildCommand(roles: Array.Empty<string>());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorCode("INVALID_ROLE");
    }

    [Fact]
    public void Roles_WhenNull_ShouldHaveError()
    {
        var command = new RegisterCommand(
            "test@test.com",
            "username",
            "John",
            "Doe",
            "Password123",
            null,
            ValidAcceptedPolicies);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorCode("INVALID_ROLE");
    }

    [Fact]
    public void Roles_WhenInvalidRole_ShouldHaveError()
    {
        var command = BuildCommand(roles: ["admin"]);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorCode("INVALID_ROLE");
    }

    [Fact]
    public void Roles_WhenDuplicate_ShouldHaveError()
    {
        var command = BuildCommand(roles: ["learner", "learner"]);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorCode("INVALID_ROLE");
    }

    [Fact]
    public void AcceptedPolicies_WhenNull_ShouldHaveError()
    {
        var command = new RegisterCommand(
            "test@test.com",
            "username",
            "John",
            "Doe",
            "Password123",
            ["learner"],
            null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AcceptedPolicies)
            .WithErrorCode("POLICY_VERSION_INVALID");
    }

    [Fact]
    public void AcceptedPolicies_WhenMissingRequiredType_ShouldHaveError()
    {
        var command = BuildCommand(acceptedPolicies:
        [
            new PolicyAcceptanceInput("terms", "2026-05-10.v1"),
            new PolicyAcceptanceInput("privacy", "2026-05-10.v1")
        ]);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AcceptedPolicies)
            .WithErrorCode("POLICY_VERSION_INVALID");
    }

    [Fact]
    public void AcceptedPolicies_WhenUnsupportedType_ShouldHaveError()
    {
        var command = BuildCommand(acceptedPolicies:
        [
            new PolicyAcceptanceInput("terms", "2026-05-10.v1"),
            new PolicyAcceptanceInput("privacy", "2026-05-10.v1"),
            new PolicyAcceptanceInput("community_guidelines", "2026-05-10.v1")
        ]);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("AcceptedPolicies[2].PolicyType");
    }

    [Fact]
    public void AcceptedPolicies_WhenDuplicateType_ShouldHaveError()
    {
        var command = BuildCommand(acceptedPolicies:
        [
            new PolicyAcceptanceInput("terms", "2026-05-10.v1"),
            new PolicyAcceptanceInput("privacy", "2026-05-10.v1"),
            new PolicyAcceptanceInput("privacy", "2026-05-10.v1")
        ]);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AcceptedPolicies)
            .WithErrorCode("POLICY_VERSION_INVALID");
    }

    private static RegisterCommand BuildCommand(
        string email = "test@test.com",
        string username = "username",
        string password = "Password123",
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<PolicyAcceptanceInput>? acceptedPolicies = null)
        => new(
            email,
            username,
            "John",
            "Doe",
            password,
            roles ?? ["learner"],
            acceptedPolicies ?? ValidAcceptedPolicies);
}
