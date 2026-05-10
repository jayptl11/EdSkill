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
        var command = new UpdateMyProfileCommand(
            true, "John@Doe",
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorCode("INVALID_DISPLAY_NAME");
    }

    [Fact]
    public void Validate_WhenPhoneFormatInvalid_ShouldHaveError()
    {
        var command = new UpdateMyProfileCommand(
            false, null,
            false, null,
            false, null,
            true, "abc",
            false, null,
            false, null,
            false, null,
            false, null,
            false, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorCode("INVALID_PHONE");
    }

    [Fact]
    public void Validate_WhenSkillsContainDuplicateValues_ShouldHaveError()
    {
        var command = new UpdateMyProfileCommand(
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            true, new[] { "C#", "c#" },
            false, null,
            false, null,
            false, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SkillsToTeach)
            .WithErrorCode("INVALID_SKILLS_TO_TEACH");
    }

    [Fact]
    public void Validate_WhenAvatarUrlUsesConfiguredBase_ShouldNotHaveError()
    {
        var command = new UpdateMyProfileCommand(
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            true, "https://cdn.edskill.test/avatar/123/file.jpg",
            false, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.AvatarUrl);
    }

    [Fact]
    public void Validate_WhenDegreeUrlUsesConfiguredBase_ShouldNotHaveError()
    {
        var command = new UpdateMyProfileCommand(
            false, null,
            false, null,
            false, null,
            false, null,
            true, "https://cdn.edskill.test/degree/123/file.pdf",
            false, null,
            false, null,
            false, null,
            false, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DegreeUrl);
    }
}
