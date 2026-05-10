namespace EdSkill.Application.Common.Models;

public record ObjectStorageUploadUrl(
    string UploadUrl,
    string PublicUrl,
    string ObjectKey,
    DateTime ExpiresAt
);
