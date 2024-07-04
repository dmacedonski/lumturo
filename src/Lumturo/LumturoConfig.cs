namespace Lumturo;

public class LumturoConfig
{
    public bool OnlyWithLabel { get; init; } = false;
    public TimeSpan ScanPeriod { get; init; } = TimeSpan.FromHours(1);
}
