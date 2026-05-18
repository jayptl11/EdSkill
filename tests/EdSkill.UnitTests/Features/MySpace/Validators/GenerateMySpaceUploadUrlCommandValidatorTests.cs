using EdSkill.Application.Features.MySpace.Commands.GenerateMySpaceUploadUrl;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.MySpace.Validators;

public class GenerateMySpaceUploadUrlCommandValidatorTests
{
    private readonly GenerateMySpaceUploadUrlCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCoverContentTypeIsPdf_ShouldHaveError()
    {
        var command = new GenerateMySpaceUploadUrlCommand(
            MySpaceUploadKind.Cover,
            "cover.pdf",
            "application/pdf",
            1024);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("INVALID_MY_SPACE_UPLOAD_CONTENT_TYPE");
    }

    [Fact]
    public void Validate_WhenCredentialFileTooLarge_ShouldHaveError()
    {
        var command = new GenerateMySpaceUploadUrlCommand(
            MySpaceUploadKind.Credential,
            "cert.pdf",
            "application/pdf",
            11 * 1024 * 1024);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("INVALID_MY_SPACE_UPLOAD_FILE_SIZE");
    }
}
