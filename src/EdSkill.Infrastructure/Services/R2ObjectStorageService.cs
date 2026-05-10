using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace EdSkill.Infrastructure.Services;

public class R2ObjectStorageService : IObjectStorageService
{
    private readonly R2StorageSettings _settings;
    private readonly IAmazonS3 _s3Client;
    private readonly Uri _publicBaseUri;

    public R2ObjectStorageService(IOptions<R2StorageSettings> settings)
    {
        _settings = settings.Value;
        ValidateSettings(_settings);

        var credentials = new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        _s3Client = new AmazonS3Client(credentials, config);
        _publicBaseUri = new Uri(_settings.PublicBaseUrl.TrimEnd('/') + "/");
    }

    public Task<ObjectStorageUploadUrl> CreateUploadUrlAsync(
        ObjectStorageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var preSignedUrl = _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = request.ObjectKey,
            Verb = HttpVerb.PUT,
            Expires = request.ExpiresAt,
            ContentType = request.ContentType
        });

        return Task.FromResult(new ObjectStorageUploadUrl(
            preSignedUrl,
            BuildPublicUrl(request.ObjectKey),
            request.ObjectKey,
            request.ExpiresAt));
    }

    public bool IsPublicUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var candidateUri))
        {
            return false;
        }

        return _publicBaseUri.IsBaseOf(candidateUri);
    }

    private string BuildPublicUrl(string objectKey)
    {
        return new Uri(_publicBaseUri, objectKey).ToString();
    }

    private static void ValidateSettings(R2StorageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AccountId) ||
            string.IsNullOrWhiteSpace(settings.AccessKeyId) ||
            string.IsNullOrWhiteSpace(settings.SecretAccessKey) ||
            string.IsNullOrWhiteSpace(settings.BucketName) ||
            string.IsNullOrWhiteSpace(settings.PublicBaseUrl))
        {
            throw new InvalidOperationException("R2 storage settings are not configured correctly.");
        }
    }
}
