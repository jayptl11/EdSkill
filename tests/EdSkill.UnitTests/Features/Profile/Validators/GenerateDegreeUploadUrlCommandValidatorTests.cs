using EdSkill.Application.Features.Profile.Commands.GenerateDegreeUploadUrl;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Profile.Validators;

public class GenerateDegreeUploadUrlCommandValidatorTests
{
    private readonly GenerateDegreeUploadUrlCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenContentTypeUnsupported_ShouldHaveError()
    {
        var command = new GenerateDegreeUploadUrlCommand("degree.exe", "application/octet-stream", 1024);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContentType)
            .WithErrorCode("INVALID_DEGREE_CONTENT_TYPE");
    }

    [Fact]
    public void Validate_WhenFileSizeTooLarge_ShouldHaveError()
    {
        var command = new GenerateDegreeUploadUrlCommand("degree.pdf", "application/pdf", 11 * 1024 * 1024);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileSize)
            .WithErrorCode("INVALID_DEGREE_FILE_SIZE");
    }
}
