using System.Text.RegularExpressions;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Commands.GenerateAchievementIconUploadUrl;

public partial class GenerateAchievementIconUploadUrlCommandHandler : IRequestHandler<GenerateAchievementIconUploadUrlCommand, Result<AchievementIconUploadUrlDto>>
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypeExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly ICurrentUserService _currentUserService;
    private readonly IObjectStorageService _objectStorageService;

    public GenerateAchievementIconUploadUrlCommandHandler(
        ICurrentUserService currentUserService,
        IObjectStorageService objectStorageService)
    {
        _currentUserService = currentUserService;
        _objectStorageService = objectStorageService;
    }

    public async Task<Result<AchievementIconUploadUrlDto>> Handle(GenerateAchievementIconUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var objectKey = BuildObjectKey(userId, request.FileName, request.ContentType);
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var uploadUrl = await _objectStorageService.CreateUploadUrlAsync(
            new ObjectStorageUploadRequest(objectKey, request.ContentType, expiresAt),
            cancellationToken);

        return Result<AchievementIconUploadUrlDto>.Success(new AchievementIconUploadUrlDto(
            uploadUrl.UploadUrl,
            uploadUrl.PublicUrl,
            uploadUrl.ObjectKey,
            uploadUrl.ExpiresAt));
    }

    private static string BuildObjectKey(Guid userId, string fileName, string contentType)
    {
        var safeName = Path.GetFileNameWithoutExtension(fileName);
        safeName = string.IsNullOrWhiteSpace(safeName)
            ? "achievement-icon"
            : InvalidFileNameCharactersRegex().Replace(safeName.Trim().ToLowerInvariant(), "-").Trim('-');

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "achievement-icon";
        }

        return $"achievement/{userId:D}/{Guid.NewGuid():N}-{safeName}{ContentTypeExtensions[contentType]}";
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex InvalidFileNameCharactersRegex();
}
