using FluentValidation;

namespace EdSkill.Application.Features.Profile.Commands.GenerateDegreeUploadUrl;

public class GenerateDegreeUploadUrlCommandValidator : AbstractValidator<GenerateDegreeUploadUrlCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    };

    private const long MaxDegreeFileSizeBytes = 10 * 1024 * 1024;

    public GenerateDegreeUploadUrlCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .WithErrorCode("INVALID_DEGREE_FILE_NAME");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("Content type is required")
            .WithErrorCode("INVALID_DEGREE_CONTENT_TYPE")
            .Must(contentType => AllowedContentTypes.Contains(contentType))
            .WithMessage("Unsupported degree content type")
            .WithErrorCode("INVALID_DEGREE_CONTENT_TYPE");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .WithMessage("File size must be greater than zero")
            .WithErrorCode("INVALID_DEGREE_FILE_SIZE")
            .LessThanOrEqualTo(MaxDegreeFileSizeBytes)
            .WithMessage("Degree file size exceeds the maximum allowed size")
            .WithErrorCode("INVALID_DEGREE_FILE_SIZE");
    }
}
