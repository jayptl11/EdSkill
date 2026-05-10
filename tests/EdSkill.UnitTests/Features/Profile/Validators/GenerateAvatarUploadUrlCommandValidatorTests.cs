using EdSkill.Application.Features.Profile.Commands.GenerateAvatarUploadUrl;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Profile.Validators;

public class GenerateAvatarUploadUrlCommandValidatorTests
{
    private readonly GenerateAvatarUploadUrlCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenContentTypeUnsupported_ShouldHaveError()
    {
        var command = new GenerateAvatarUploadUrlCommand("avatar.gif", "image/gif", 1024);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContentType)
            .WithErrorCode("INVALID_AVATAR_CONTENT_TYPE");
    }

    [Fact]
    public void Validate_WhenFileSizeTooLarge_ShouldHaveError()
    {
        var command = new GenerateAvatarUploadUrlCommand("avatar.png", "image/png", 6 * 1024 * 1024);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileSize)
            .WithErrorCode("INVALID_AVATAR_FILE_SIZE");
    }
}
