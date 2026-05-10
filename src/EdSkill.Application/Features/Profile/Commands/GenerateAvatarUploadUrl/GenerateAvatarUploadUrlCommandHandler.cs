using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;
using System.Text.RegularExpressions;

namespace EdSkill.Application.Features.Profile.Commands.GenerateAvatarUploadUrl;

public partial class GenerateAvatarUploadUrlCommandHandler : IRequestHandler<GenerateAvatarUploadUrlCommand, Result<AvatarUploadUrlDto>>
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypeExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly ICurrentUserService _currentUserService;
    private readonly IObjectStorageService _objectStorageService;

    public GenerateAvatarUploadUrlCommandHandler(
        ICurrentUserService currentUserService,
        IObjectStorageService objectStorageService)
    {
        _currentUserService = currentUserService;
        _objectStorageService = objectStorageService;
    }

    public async Task<Result<AvatarUploadUrlDto>> Handle(GenerateAvatarUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var objectKey = BuildObjectKey(userId, request.FileName, request.ContentType);
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var uploadUrl = await _objectStorageService.CreateUploadUrlAsync(
            new ObjectStorageUploadRequest(objectKey, request.ContentType, expiresAt),
            cancellationToken);

        return Result<AvatarUploadUrlDto>.Success(new AvatarUploadUrlDto(
            uploadUrl.UploadUrl,
            uploadUrl.PublicUrl,
            uploadUrl.ObjectKey,
            uploadUrl.ExpiresAt));
    }

    private static string BuildObjectKey(Guid userId, string fileName, string contentType)
    {
        var safeName = Path.GetFileNameWithoutExtension(fileName);
        safeName = string.IsNullOrWhiteSpace(safeName)
            ? "avatar"
            : InvalidFileNameCharactersRegex().Replace(safeName.Trim().ToLowerInvariant(), "-").Trim('-');

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "avatar";
        }

        var extension = ContentTypeExtensions[contentType];

        return $"avatar/{userId:D}/{Guid.NewGuid():N}-{safeName}{extension}";
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex InvalidFileNameCharactersRegex();
}
