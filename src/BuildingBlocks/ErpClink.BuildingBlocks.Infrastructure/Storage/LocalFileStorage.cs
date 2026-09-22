using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.Application.Options;
using Microsoft.Extensions.Options;

namespace ErpClink.BuildingBlocks.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<FileStorageOptions> options)
    {
        var configured = options.Value.LocalRootPath;
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));

        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            throw new AppException("storage.not_found", "Stored file was not found.", 404);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        return Task.FromResult(File.Exists(path));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new AppException("storage.invalid_key", "Storage key is required.", 400);
        }

        var normalized = storageKey.Replace('\\', '/').Trim('/');
        if (normalized.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(normalized))
        {
            throw new AppException("storage.invalid_key", "Storage key is invalid.", 400);
        }

        var full = Path.GetFullPath(Path.Combine(_root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("storage.invalid_key", "Storage key is invalid.", 400);
        }

        return full;
    }
}
