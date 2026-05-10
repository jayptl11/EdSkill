using FluentValidation;

namespace EdSkill.Application.Features.Profile.Commands.GenerateAvatarUploadUrl;

public class GenerateAvatarUploadUrlCommandValidator : AbstractValidator<GenerateAvatarUploadUrlCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const long MaxAvatarFileSizeBytes = 5 * 1024 * 1024;

    public GenerateAvatarUploadUrlCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .WithErrorCode("INVALID_AVATAR_FILE_NAME");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("Content type is required")
            .WithErrorCode("INVALID_AVATAR_CONTENT_TYPE")
            .Must(contentType => AllowedContentTypes.Contains(contentType))
            .WithMessage("Unsupported avatar content type")
            .WithErrorCode("INVALID_AVATAR_CONTENT_TYPE");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .WithMessage("File size must be greater than zero")
            .WithErrorCode("INVALID_AVATAR_FILE_SIZE")
            .LessThanOrEqualTo(MaxAvatarFileSizeBytes)
            .WithMessage("Avatar file size exceeds the maximum allowed size")
            .WithErrorCode("INVALID_AVATAR_FILE_SIZE");
    }
}
