using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.FileStorage;

/// <summary>
/// Saves files to local disk under a configured root, and hands back a URL served via static files
/// (see <see cref="ServiceDefaultsExtensions.UseLocalFileStorage"/>). Swap for a blob-storage
/// implementation of <see cref="IFileStorageService"/> without touching callers.
/// </summary>
public class LocalDiskFileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private readonly string _publicBaseUrl;

    public LocalDiskFileStorageService(IConfiguration configuration)
    {
        var configuredRoot = configuration["FileStorage:RootPath"] ?? "App_Data/uploads";
        _rootPath = Path.IsPathRooted(configuredRoot) ? configuredRoot : Path.Combine(AppContext.BaseDirectory, configuredRoot);
        _publicBaseUrl = (configuration["FileStorage:PublicBaseUrl"] ?? "/uploads").TrimEnd('/');
    }

    public async Task<FileStorageResult> SaveAsync(
        string containerName, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var safeFileName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var containerPath = Path.Combine(_rootPath, containerName);
        Directory.CreateDirectory(containerPath);

        var fullPath = Path.Combine(containerPath, safeFileName);
        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var relativePath = $"{containerName}/{safeFileName}";
        return new FileStorageResult(relativePath, $"{_publicBaseUrl}/{relativePath}");
    }
}
