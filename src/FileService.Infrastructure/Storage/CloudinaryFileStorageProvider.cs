using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FileService.Application.Abstractions.Storage;
using FileService.Domain.Enums;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.Storage;

public sealed class CloudinaryFileStorageProvider : IFileStorageProvider
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryStorageOptions _options;

    public CloudinaryFileStorageProvider(IOptions<CloudinaryStorageOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.CloudName)
            || string.IsNullOrWhiteSpace(_options.ApiKey)
            || string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary configuration is missing.");
        }

        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    public FileProvider Provider => FileProvider.Cloudinary;

    public async Task<FileStorageUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = NormalizeFolder(_options.StagingFolder),
            UseFilename = true,
            UniqueFilename = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.StatusCode != System.Net.HttpStatusCode.OK
            && result.StatusCode != System.Net.HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error?.Message}");
        }

        return new FileStorageUploadResult(
            result.PublicId,
            null,
            null,
            result.Bytes,
            result.Format);
    }

    public async Task<FileStoragePromotionResult> PromoteAsync(
        string stagedProviderKey,
        string? destinationFolder,
        CancellationToken cancellationToken = default)
    {
        var targetFolder = string.IsNullOrWhiteSpace(destinationFolder)
            ? _options.PublicFolder
            : destinationFolder;

        var newPublicId = BuildTargetPublicId(stagedProviderKey, targetFolder);

        var renameParams = new RenameParams(stagedProviderKey, newPublicId)
        {
            Overwrite = true,
            ResourceType = ResourceType.Raw
        };

        var result = await _cloudinary.RenameAsync(renameParams);

        if (result.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Cloudinary promote failed: {result.Error?.Message}");
        }

        return new FileStoragePromotionResult(result.PublicId, null);
    }

    public async Task DeleteAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        var destroyParams = new DeletionParams(providerKey) { ResourceType = ResourceType.Raw };
        var result = await _cloudinary.DestroyAsync(destroyParams);

        if (result.StatusCode != System.Net.HttpStatusCode.OK
            && result.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Cloudinary delete failed: {result.Error?.Message}");
        }
    }

    public Task<Uri> GetDownloadUrlAsync(
        string providerKey,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default)
    {
        var url = _cloudinary.Api.Url
            .ResourceType("raw")
            .Secure(true)
            .BuildUrl(providerKey);
        return Task.FromResult(new Uri(url));
    }

    private static string NormalizeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }

        return folder.Trim().Trim('/');
    }

    private static string BuildTargetPublicId(string sourcePublicId, string? folder)
    {
        var name = sourcePublicId.Split('/').Last();
        var normalized = NormalizeFolder(folder);

        return string.IsNullOrWhiteSpace(normalized)
            ? name
            : $"{normalized}/{name}";
    }
}
