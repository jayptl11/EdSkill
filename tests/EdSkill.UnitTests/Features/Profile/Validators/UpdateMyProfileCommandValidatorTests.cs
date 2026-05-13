using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Profile.Commands.UpdateMyProfile;
using FluentValidation.TestHelper;
using Moq;

namespace EdSkill.UnitTests.Features.Profile.Validators;

public class UpdateMyProfileCommandValidatorTests
{
    private readonly UpdateMyProfileCommandValidator _validator;

    public UpdateMyProfileCommandValidatorTests()
    {
        var objectStorageService = new Mock<IObjectStorageService>();
        objectStorageService
            .Setup(x => x.IsPublicUrl(It.IsAny<string>()))
            .Returns<string>(url => url.StartsWith("https://cdn.edskill.test/", StringComparison.OrdinalIgnoreCase));

        _validator = new UpdateMyProfileCommandValidator(objectStorageService.Object);
    }

    [Fact]
    public void Validate_WhenDisplayNameContainsUnsupportedCharacters_ShouldHaveError()
    {
        var command = NewCommand(hasDisplayName: true, displayName: "John@Doe");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorCode("INVALID_DISPLAY_NAME");
    }

    [Fact]
    public void Validate_WhenPhoneFormatInvalid_ShouldHaveError()
    {
        var command = NewCommand(hasPhone: true, phone: "abc");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorCode("INVALID_PHONE");
    }

    [Fact]
    public void Validate_WhenSkillsContainDuplicateValues_ShouldHaveError()
    {
        var command = NewCommand(hasSkillsToTeach: true, skillsToTeach: new[] { "C#", "c#" });

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SkillsToTeach)
            .WithErrorCode("INVALID_SKILLS_TO_TEACH");
    }

    [Fact]
    public void Validate_WhenAvatarUrlUsesConfiguredBase_ShouldNotHaveError()
    {
        var command = NewCommand(hasAvatarUrl: true, avatarUrl: "https://cdn.edskill.test/avatar/123/file.jpg");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.AvatarUrl);
    }

    [Fact]
    public void Validate_WhenCredentialUrlsUseConfiguredBase_ShouldNotHaveError()
    {
        var command = NewCommand(
            hasCredentialUrls: true,
            credentialUrls: new[] { "https://cdn.edskill.test/degree/123/file.pdf", "https://cdn.edskill.test/degree/123/file-2.pdf" });

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.CredentialUrls);
    }

    private static UpdateMyProfileCommand NewCommand(
        bool hasDisplayName = false,
        string? displayName = null,
        bool hasBio = false,
        string? bio = null,
        bool hasDateOfBirth = false,
        DateTime? dateOfBirth = null,
        bool hasPhone = false,
        string? phone = null,
        bool hasDegreeUrl = false,
        string? degreeUrl = null,
        bool hasCredentialUrls = false,
        IReadOnlyCollection<string>? credentialUrls = null,
        bool hasSkillsToTeach = false,
        IReadOnlyCollection<string>? skillsToTeach = null,
        bool hasSkillsToLearn = false,
        IReadOnlyCollection<string>? skillsToLearn = null,
        bool hasAvatarUrl = false,
        string? avatarUrl = null,
        bool hasIsPublic = false,
        bool? isPublic = null)
    {
        return new UpdateMyProfileCommand(
            hasDisplayName, displayName,
            hasBio, bio,
            hasDateOfBirth, dateOfBirth,
            hasPhone, phone,
            hasDegreeUrl, degreeUrl,
            hasCredentialUrls, credentialUrls,
            hasSkillsToTeach, skillsToTeach,
            hasSkillsToLearn, skillsToLearn,
            hasAvatarUrl, avatarUrl,
            hasIsPublic, isPublic);
    }
}
