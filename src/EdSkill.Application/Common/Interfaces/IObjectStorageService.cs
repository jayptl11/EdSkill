using EdSkill.Application.Common.Models;

namespace EdSkill.Application.Common.Interfaces;

public interface IObjectStorageService
{
    Task<ObjectStorageUploadUrl> CreateUploadUrlAsync(
        ObjectStorageUploadRequest request,
        CancellationToken cancellationToken = default);

    bool IsPublicUrl(string url);
}
