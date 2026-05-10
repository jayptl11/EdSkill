namespace EdSkill.Application.Common.Models;

public record ObjectStorageUploadRequest(
    string ObjectKey,
    string ContentType,
    DateTime ExpiresAt
);
