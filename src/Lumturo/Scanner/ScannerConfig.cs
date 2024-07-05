namespace Lumturo.Scanner;

public class ScannerConfig
{
    public TimeSpan Period { get; init; } = TimeSpan.FromHours(1);
    public Dictionary<string, bool> Filter { get; init; } = [];
}
