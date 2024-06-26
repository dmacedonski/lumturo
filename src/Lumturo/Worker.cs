using Humanizer;

using Microsoft.Extensions.Options;

namespace Lumturo;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IScannerProvider _scanner;
    private LumturoConfig _config;
    private readonly IDisposable? _configMonitorDisposable;

    public Worker(ILogger<Worker> logger, IOptionsMonitor<LumturoConfig> optionsMonitor, IScannerProvider scanner)
    {
        _logger = logger;
        _scanner = scanner;
        _config = optionsMonitor.CurrentValue;
        _configMonitorDisposable = optionsMonitor.OnChange(ConfigChangedHandler);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        _logger.LogInformation("Background worker starting...");
        _logger.LogInformation("Scan level is set to {ScanLevel} with period to {ScanPeriod}", _config.ScanLevel, _config.ScanPeriod.Humanize());
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _scanner.ScanAsync(_config.ScanLevel, stoppingToken);
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
        if (_config.ScanLevel != config.ScanLevel)
        {
            _logger.LogInformation("Scan level has been changed from {CurrentScanLevel} to {NewScanLevel}", _config.ScanLevel, config.ScanLevel);
        }
        if (_config.ScanPeriod != config.ScanPeriod)
        {
            _logger.LogInformation("Scan period has been changed from {CurrentScanPeriod} to {NewScanPeriod}", _config.ScanPeriod.Humanize(), config.ScanPeriod.Humanize());
        }
        _config = config;
    }
}
