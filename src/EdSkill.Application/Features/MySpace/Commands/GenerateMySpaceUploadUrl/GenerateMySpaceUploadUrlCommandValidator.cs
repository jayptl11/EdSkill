using FluentValidation;

namespace EdSkill.Application.Features.MySpace.Commands.GenerateMySpaceUploadUrl;

public class GenerateMySpaceUploadUrlCommandValidator : AbstractValidator<GenerateMySpaceUploadUrlCommand>
{
    private static readonly HashSet<string> CoverContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly HashSet<string> CredentialContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    };

    public GenerateMySpaceUploadUrlCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.FileSize).GreaterThan(0);

        RuleFor(x => x)
            .Must(HaveSupportedContentType)
            .WithErrorCode("INVALID_MY_SPACE_UPLOAD_CONTENT_TYPE")
            .WithMessage("Upload content type is invalid.");

        RuleFor(x => x)
            .Must(HaveSupportedFileSize)
            .WithErrorCode("INVALID_MY_SPACE_UPLOAD_FILE_SIZE")
            .WithMessage("Upload file size is invalid.");
    }

    private static bool HaveSupportedContentType(GenerateMySpaceUploadUrlCommand command)
    {
        return command.Kind switch
        {
            MySpaceUploadKind.Cover => CoverContentTypes.Contains(command.ContentType),
            MySpaceUploadKind.Credential => CredentialContentTypes.Contains(command.ContentType),
            _ => false
        };
    }

    private static bool HaveSupportedFileSize(GenerateMySpaceUploadUrlCommand command)
    {
        var maxBytes = command.Kind == MySpaceUploadKind.Cover
            ? 5 * 1024 * 1024
            : 10 * 1024 * 1024;

        return command.FileSize <= maxBytes;
    }
}
