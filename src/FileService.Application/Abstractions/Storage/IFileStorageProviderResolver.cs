using FileService.Domain.Enums;

namespace FileService.Application.Abstractions.Storage;

public interface IFileStorageProviderResolver
{
    IFileStorageProvider Resolve(FileProvider provider);

    IFileStorageProvider ResolveDefault();
}
