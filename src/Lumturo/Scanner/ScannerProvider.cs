using Docker.DotNet;
using Docker.DotNet.Models;

using Lumturo.Docker.Registry;

namespace Lumturo.Scanner;

public interface IScannerProvider
{
    Task SyncAsync(IDictionary<string, bool> filter, CancellationToken cancellationToken = default);
    Task CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}

public class ScannerProvider(ILogger<ScannerProvider> logger, IDockerRegistryProvider dockerRegistryProvider) : IScannerProvider
{
    private readonly DockerClient _dockerClient = new DockerClientConfiguration().CreateClient();
    private readonly HashSet<ScannerCacheItem> _cache = [];

    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in _cache)
        {
            foreach (var name in item.ImageNames)
            {
                try
                {
                    item.State = await dockerRegistryProvider.CheckIsLatestAsync(name, item.ImageId, cancellationToken) ? State.UpToDate : State.OutOfDate;
                }
                catch (Exception ex)
                {
                    logger.LogDebug("Could not check if the image is up to date: {Message}", ex.Message);
                }
            }

            if (item.State == State.New)
                item.State = State.Unknown;
        }

        logger.LogInformation("Checking updates finished: {UpToDate} up-to-date, {OutOfDate} out-of-date, {Unknown} unknown", _cache.Count(i => i.State == State.UpToDate), _cache.Count(i => i.State == State.OutOfDate), _cache.Count(i => i.State == State.Unknown));
    }

    public async Task SyncAsync(IDictionary<string, bool> filter, CancellationToken cancellationToken = default)
    {
        var added = 0;
        var removed = 0;

        try
        {
            var parameters = new ContainersListParameters { All = true };
            var mustHave = filter.Where(i => i.Value).Select(i => i.Key);
            var canNotHave = filter.Where(i => !i.Value).Select(i => i.Key);

            if (filter.Any())
                parameters.Filters = new Dictionary<string, IDictionary<string, bool>> { { "name", mustHave.ToDictionary(i => i, i => true) } };

            var containerListResponses = await _dockerClient.Containers.ListContainersAsync(parameters, cancellationToken);

            foreach (var containerListResponse in containerListResponses)
            {
                var containerInspectResponse = await _dockerClient.Containers.InspectContainerAsync(containerListResponse.ID, cancellationToken);
                var imageInspectResponse = await _dockerClient.Images.InspectImageAsync(containerInspectResponse.Image, cancellationToken);
                var uniqueImageNames = new HashSet<string>();
                foreach (var repoTag in imageInspectResponse.RepoTags)
                {
                    var split = repoTag.Split(':');

                    if (split.Length != 2)
                        continue;

                    uniqueImageNames.Add(split[0]);
                }
                if (!canNotHave.Contains(containerInspectResponse.Name.TrimStart('/')) && _cache.Add(new ScannerCacheItem { ContainerId = containerInspectResponse.ID, ContainerName = containerInspectResponse.Name.TrimStart('/'), ImageId = imageInspectResponse.ID, ImageNames = [.. uniqueImageNames] }))
                    added++;
            }

            foreach (var cacheItem in _cache)
            {
                if (containerListResponses.Any(i => i.ID == cacheItem.ContainerId) && !canNotHave.Contains(cacheItem.ContainerName))
                    continue;

                if (_cache.Remove(cacheItem))
                    removed++;
            }

            logger.LogInformation("Synchronization finished: {Added} added, {Removed} removed, {Other} without changes", added, removed, _cache.Count - added);
        }
        catch (Exception ex)
        {
            logger.LogError("Synchronization failed: {Message}", ex.Message);
        }
    }
}
