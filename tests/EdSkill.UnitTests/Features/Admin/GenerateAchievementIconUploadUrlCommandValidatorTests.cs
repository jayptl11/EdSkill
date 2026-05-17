using EdSkill.Application.Features.Admin.Commands.GenerateAchievementIconUploadUrl;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Admin;

public class GenerateAchievementIconUploadUrlCommandValidatorTests
{
    private readonly GenerateAchievementIconUploadUrlCommandValidator _validator = new();

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/jpg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void Validate_WhenContentTypeIsSupported_DoesNotReturnError(string contentType)
    {
        var command = new GenerateAchievementIconUploadUrlCommand("badge.png", contentType, 1024);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("image/svg+xml")]
    [InlineData("application/octet-stream")]
    [InlineData("")]
    public void Validate_WhenContentTypeIsUnsupported_ReturnsError(string contentType)
    {
        var command = new GenerateAchievementIconUploadUrlCommand("badge.png", contentType, 1024);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContentType)
            .WithErrorCode("INVALID_ACHIEVEMENT_ICON_CONTENT_TYPE");
    }

    [Fact]
    public void Validate_WhenFileSizeExceeds10Mb_ReturnsError()
    {
        var command = new GenerateAchievementIconUploadUrlCommand("badge.png", "image/png", 10 * 1024 * 1024 + 1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileSize)
            .WithErrorCode("INVALID_ACHIEVEMENT_ICON_FILE_SIZE");
    }

    [Fact]
    public void Validate_WhenFileSizeIsExactly10Mb_DoesNotReturnError()
    {
        var command = new GenerateAchievementIconUploadUrlCommand("badge.png", "image/png", 10 * 1024 * 1024);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.FileSize);
    }
}
