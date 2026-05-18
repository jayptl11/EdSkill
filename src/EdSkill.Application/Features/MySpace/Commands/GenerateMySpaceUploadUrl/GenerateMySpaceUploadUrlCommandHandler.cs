using System.Text.RegularExpressions;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using MediatR;

namespace EdSkill.Application.Features.MySpace.Commands.GenerateMySpaceUploadUrl;

public partial class GenerateMySpaceUploadUrlCommandHandler : IRequestHandler<GenerateMySpaceUploadUrlCommand, Result<MySpaceUploadUrlDto>>
{
    private static readonly IReadOnlyDictionary<string, string> CoverContentTypeExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private static readonly IReadOnlyDictionary<string, string> CredentialContentTypeExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["application/pdf"] = ".pdf"
    };

    private readonly ICurrentUserService _currentUserService;
    private readonly IObjectStorageService _objectStorageService;

    public GenerateMySpaceUploadUrlCommandHandler(
        ICurrentUserService currentUserService,
        IObjectStorageService objectStorageService)
    {
        _currentUserService = currentUserService;
        _objectStorageService = objectStorageService;
    }

    public async Task<Result<MySpaceUploadUrlDto>> Handle(GenerateMySpaceUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var objectKey = BuildObjectKey(userId, request.Kind, request.FileName, request.ContentType);
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var uploadUrl = await _objectStorageService.CreateUploadUrlAsync(
            new ObjectStorageUploadRequest(objectKey, request.ContentType, expiresAt),
            cancellationToken);

        return Result<MySpaceUploadUrlDto>.Success(new MySpaceUploadUrlDto(
            uploadUrl.UploadUrl,
            uploadUrl.PublicUrl,
            uploadUrl.ObjectKey,
            uploadUrl.ExpiresAt));
    }

    private static string BuildObjectKey(Guid userId, MySpaceUploadKind kind, string fileName, string contentType)
    {
        var safeName = Path.GetFileNameWithoutExtension(fileName);
        safeName = string.IsNullOrWhiteSpace(safeName)
            ? kind.ToString().ToLowerInvariant()
            : InvalidFileNameCharactersRegex().Replace(safeName.Trim().ToLowerInvariant(), "-").Trim('-');

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = kind.ToString().ToLowerInvariant();
        }

        var extension = kind == MySpaceUploadKind.Cover
            ? CoverContentTypeExtensions[contentType]
            : CredentialContentTypeExtensions[contentType];
        var segment = kind == MySpaceUploadKind.Cover ? "cover" : "credential";

        return $"my-space/{segment}/{userId:D}/{Guid.NewGuid():N}-{safeName}{extension}";
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex InvalidFileNameCharactersRegex();
}
