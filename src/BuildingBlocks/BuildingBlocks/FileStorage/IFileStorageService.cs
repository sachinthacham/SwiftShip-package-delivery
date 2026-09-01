namespace BuildingBlocks.FileStorage;

public record FileStorageResult(string RelativePath, string Url);

public interface IFileStorageService
{
    Task<FileStorageResult> SaveAsync(
        string containerName, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);
}
