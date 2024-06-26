using Docker.DotNet;
using Docker.DotNet.Models;

namespace Lumturo;

public interface IScannerProvider
{
    Task ScanAsync(ScanLevel level, CancellationToken cancellationToken = default);
}

public class ScannerProvider(ILogger<ScannerProvider> logger) : IScannerProvider, IDisposable
{
    private bool _disposed;
    private readonly DockerClient _dockerClient = new DockerClientConfiguration().CreateClient();
    private readonly Dictionary<string, ScannerCacheItem> _cache = [];

    public async Task ScanAsync(ScanLevel level, CancellationToken cancellationToken = default)
    {
        string[] imagesIds = level == ScanLevel.All
            ? []
            : (await GetContainersAsync(level == ScanLevel.Marked, cancellationToken))
                .Select(container => container.ImageID)
                .ToArray();
        IList<ImagesListResponse> images = await GetImagesAsync(imagesIds, cancellationToken);
        int added = 0;
        int removed = 0;
        foreach (ImagesListResponse image in images)
        {
            if (!_cache.TryGetValue(image.ID, out ScannerCacheItem? item))
            {
                item = new ScannerCacheItem()
                {
                    RepoTags = [.. image.RepoTags]
                };
                _cache[image.ID] = item;
                added++;
            }
        }
        foreach (string imageId in _cache.Keys)
        {
            if (!images.Any(image => image.ID == imageId))
            {
                _cache.Remove(imageId);
                removed++;
            }
        }
        logger.LogInformation("Scan result, added: {Added}, removed: {Removed} images", added, removed);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            _dockerClient.Dispose();
        }
        _disposed = true;
    }

    private async Task<IList<ImagesListResponse>> GetImagesAsync(string[] imagesIds, CancellationToken cancellationToken = default)
    {
        ImagesListParameters parameters = new();
        IList<ImagesListResponse> images = await _dockerClient.Images.ListImagesAsync(parameters, cancellationToken);
        if (imagesIds.Length != 0)
        {
            images = images.Where(image => imagesIds.Contains(image.ID)).ToList();
        }
        return images;
    }

    private Task<IList<ContainerListResponse>> GetContainersAsync(bool marked, CancellationToken cancellationToken = default)
    {
        ContainersListParameters parameters = new()
        {
            All = true,
            Filters = marked ? new Dictionary<string, IDictionary<string, bool>>()
            {
                {"label", new Dictionary<string, bool>() { {"lumturo=true", true} }}
            } : []
        };
        return _dockerClient.Containers.ListContainersAsync(parameters, cancellationToken);
    }
}
