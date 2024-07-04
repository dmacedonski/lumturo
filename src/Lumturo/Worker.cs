using Docker.DotNet;
using Docker.DotNet.Models;

using Humanizer;

using Lumturo.Docker.Registry;

using Microsoft.Extensions.Options;

namespace Lumturo;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDockerRegistryProvider _dockerRegistryProvider;
    private LumturoConfig _config;
    private readonly IDisposable? _configMonitorDisposable;
    private readonly DockerClient _dockerClient = new DockerClientConfiguration().CreateClient();

    public Worker(ILogger<Worker> logger, IOptionsMonitor<LumturoConfig> optionsMonitor, IDockerRegistryProvider dockerRegistryProvider)
    {
        _logger = logger;
        _dockerRegistryProvider = dockerRegistryProvider;
        _config = optionsMonitor.CurrentValue;
        _configMonitorDisposable = optionsMonitor.OnChange(ConfigChangedHandler);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        _logger.LogInformation("Background worker starting...");
        _logger.LogInformation("Scan period is set to {ScanPeriod}", _config.ScanPeriod.Humanize());

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var containers = (await GetContainersAsync(_config.OnlyWithLabel, stoppingToken)).ToArray();
                List<ContainerInspectResponse> containersToUpdate = [];

                foreach (var container in containers)
                {
                    try
                    {
                        var containerInspection = await _dockerClient.Containers.InspectContainerAsync(container.ID, stoppingToken);

                        try
                        {
                            var image = await _dockerClient.Images.InspectImageAsync(container.ImageID, stoppingToken);
                            HashSet<string> imageNames = [];

                            foreach (var repoTag in image.RepoTags)
                            {
                                var match = repoTag.Split(':');
                                if (match.Length == 2)
                                {
                                    imageNames.Add(match[0]);
                                }
                            }

                            if (imageNames.Count == 0)
                            {
                                continue;
                            }

                            bool? isUpToDate = null;
                            foreach (var imageName in imageNames)
                            {
                                try
                                {
                                    isUpToDate = await _dockerRegistryProvider.CheckIsLatestAsync(imageName, image.ID, stoppingToken);
                                    if (isUpToDate == true)
                                    {
                                        goto containers_loop_end;
                                    }
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError("Check latest version for image {Name} failed: {Message}", imageName, e.Message);
                                }
                            }
                            if (isUpToDate == false)
                            {
                                containersToUpdate.Add(containerInspection);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("Image {Id} inspection failed: {Message}", container.ImageID, e.Message);
                        }
                    containers_loop_end:;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Inspect container {Id} failed: {Message}", container.ID, e.Message);
                    }
                }

                if (containersToUpdate.Count > 0)
                {
                    var containersNamesToUpdate = containersToUpdate.Select(c => c.Name.Trim('/')).ToList();
                    _logger.LogInformation("Found {Count} to update: {Names}", "container".ToQuantity(containersToUpdate.Count), string.Join(", ", containersNamesToUpdate));
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Scanning failed: {Message}", e.Message);
            }

            await Task.Delay(_config.ScanPeriod, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _configMonitorDisposable?.Dispose();
        base.Dispose();
    }

    private void ConfigChangedHandler(LumturoConfig config)
    {
        if (_config.OnlyWithLabel != config.OnlyWithLabel)
        {
            if (_config.OnlyWithLabel)
            {
                _logger.LogInformation("Configuration changed, only containers with label \"lumturo\" will be scanned");
            }
            else
            {
                _logger.LogInformation("Configuration changed, all containers will be scanned");
            }
        }
        if (_config.ScanPeriod != config.ScanPeriod)
        {
            _logger.LogInformation("Scan period has been changed from {CurrentScanPeriod} to {NewScanPeriod}", _config.ScanPeriod.Humanize(), config.ScanPeriod.Humanize());
        }
        _config = config;
    }

    private Task<IList<ContainerListResponse>> GetContainersAsync(bool marked, CancellationToken cancellationToken = default)
    {
        var parameters = new ContainersListParameters
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
