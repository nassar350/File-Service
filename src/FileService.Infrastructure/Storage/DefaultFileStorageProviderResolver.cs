using FileService.Application.Abstractions.Storage;
using FileService.Domain.Enums;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.Storage;

public sealed class DefaultFileStorageProviderResolver : IFileStorageProviderResolver
{
    private readonly IReadOnlyDictionary<FileProvider, IFileStorageProvider> _providers;
    private readonly StorageOptions _options;

    public DefaultFileStorageProviderResolver(
        IEnumerable<IFileStorageProvider> providers,
        IOptions<StorageOptions> options)
    {
        _providers = providers.ToDictionary(provider => provider.Provider);
        _options = options.Value;
    }

    public IFileStorageProvider Resolve(FileProvider provider)
    {
        var resolvedProvider = provider == FileProvider.Unknown
            ? _options.DefaultProvider
            : provider;

        if (resolvedProvider == FileProvider.Unknown)
        {
            throw new InvalidOperationException("Storage:DefaultProvider is not configured.");
        }

        if (_providers.TryGetValue(resolvedProvider, out var storageProvider))
        {
            return storageProvider;
        }

        throw new InvalidOperationException($"No storage provider registered for {resolvedProvider}.");
    }

    public IFileStorageProvider ResolveDefault()
    {
        return Resolve(_options.DefaultProvider);
    }
}
