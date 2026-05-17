using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FileService.Application.Abstractions.Storage;
using FileService.Domain.Enums;
using FileService.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.Storage;

public sealed class S3FileStorageProvider : IFileStorageProvider
{
    private readonly IAmazonS3 _s3;
    private readonly S3StorageOptions _options;

    public S3FileStorageProvider(IAmazonS3 s3, IOptions<S3StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("Storage:S3:BucketName is required.");
        }
    }

    public FileProvider Provider => FileProvider.AmazonS3;

    public async Task<FileStorageUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(_options.StagingFolder, fileName);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        var response = await _s3.PutObjectAsync(request, cancellationToken);
        var size = content.CanSeek ? content.Length : 0;

        return new FileStorageUploadResult(
            key,
            _options.Region,
            response.ETag?.Trim('"'),
            size,
            contentType);
    }

    public async Task<FileStoragePromotionResult> PromoteAsync(
        string stagedProviderKey,
        string? destinationFolder,
        CancellationToken cancellationToken = default)
    {
        var targetFolder = string.IsNullOrWhiteSpace(destinationFolder)
            ? _options.PublicFolder
            : destinationFolder;

        var fileName = stagedProviderKey.Split('/').Last();
        var destinationKey = BuildKey(targetFolder, fileName);

        var copyRequest = new CopyObjectRequest
        {
            SourceBucket = _options.BucketName,
            SourceKey = stagedProviderKey,
            DestinationBucket = _options.BucketName,
            DestinationKey = destinationKey
        };

        await _s3.CopyObjectAsync(copyRequest, cancellationToken);

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = stagedProviderKey
        };

        await _s3.DeleteObjectAsync(deleteRequest, cancellationToken);

        return new FileStoragePromotionResult(destinationKey, _options.Region);
    }

    public async Task DeleteAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = providerKey
        };

        await _s3.DeleteObjectAsync(request, cancellationToken);
    }

    public Task<Uri> GetDownloadUrlAsync(
        string providerKey,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default)
    {
        var expiry = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromMinutes(15));

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = providerKey,
            Expires = expiry
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(new Uri(url));
    }

    public static IAmazonS3 CreateClient(S3StorageOptions options)
    {
        var config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(options.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
            config.ForcePathStyle = options.ForcePathStyle;
        }

        if (!string.IsNullOrWhiteSpace(options.AccessKey)
            && !string.IsNullOrWhiteSpace(options.SecretKey))
        {
            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
            return new AmazonS3Client(credentials, config);
        }

        return new AmazonS3Client(config);
    }

    private static string BuildKey(string? folder, string fileName)
    {
        var normalized = NormalizeFolder(folder);
        var safeName = string.IsNullOrWhiteSpace(fileName)
            ? Guid.NewGuid().ToString("N")
            : Path.GetFileName(fileName);

        var uniqueName = $"{Guid.NewGuid():N}_{safeName}";

        return string.IsNullOrWhiteSpace(normalized)
            ? uniqueName
            : $"{normalized}/{uniqueName}";
    }

    private static string NormalizeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }

        return folder.Trim().Trim('/');
    }
}
