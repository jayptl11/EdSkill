using FluentValidation;

namespace EdSkill.Application.Features.Admin.Commands.GenerateAchievementIconUploadUrl;

public class GenerateAchievementIconUploadUrlCommandValidator : AbstractValidator<GenerateAchievementIconUploadUrlCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    public GenerateAchievementIconUploadUrlCommandValidator()
    {
        RuleFor(item => item.FileName)
            .NotEmpty()
            .WithErrorCode("INVALID_ACHIEVEMENT_ICON_FILE_NAME");

        RuleFor(item => item.ContentType)
            .Must(value => !string.IsNullOrWhiteSpace(value) && AllowedContentTypes.Contains(value))
            .WithMessage("Achievement icon content type is invalid.")
            .WithErrorCode("INVALID_ACHIEVEMENT_ICON_CONTENT_TYPE");

        RuleFor(item => item.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("Achievement icon file size is invalid.")
            .WithErrorCode("INVALID_ACHIEVEMENT_ICON_FILE_SIZE");
    }
}
