namespace ErpClink.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Port for binary file storage (metadata lives in modules; bytes live here).
/// </summary>
public interface IFileStorage
{
    Task SaveAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
