using Humanizer;

using Microsoft.Extensions.Options;

namespace Lumturo.Scanner;

public class ScannerWorker : BackgroundService
{
    private ScannerConfig _config;
    private readonly IDisposable? _configMonitorDisposable;
    private readonly ILogger<ScannerWorker> _logger;
    private readonly IScannerProvider _scannerProvider;

    public ScannerWorker(IOptionsMonitor<LumturoConfig> optionsMonitor, ILogger<ScannerWorker> logger, IScannerProvider scannerProvider)
    {
        _config = optionsMonitor.CurrentValue.Scanner;
        _configMonitorDisposable = optionsMonitor.OnChange(config => _config = config.Scanner);
        _logger = logger;
        _scannerProvider = scannerProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Task has been starting...");
            await _scannerProvider.SyncAsync(_config.Filter, stoppingToken);
            await _scannerProvider.CheckForUpdatesAsync(stoppingToken);
            _logger.LogInformation("The task has been completed, next launch in {Time}", DateTime.UtcNow.Add(_config.Period).Humanize());
            await Task.Delay(_config.Period, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _configMonitorDisposable?.Dispose();
        base.Dispose();
    }
}
