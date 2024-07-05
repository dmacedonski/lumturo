namespace Lumturo.Scanner;

public enum State
{
    New,
    UpToDate,
    OutOfDate,
    Unknown
}

public class ScannerCacheItem : IEquatable<ScannerCacheItem>
{
    public required string ContainerId { get; init; }
    public required string ContainerName { get; init; }
    public required string ImageId { get; init; }
    public required string[] ImageNames { get; init; }
    public State State { get; set; } = State.New;

    public bool Equals(ScannerCacheItem? other)
    {
        return other is not null && ContainerId == other.ContainerId;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ScannerCacheItem);
    }

    public override int GetHashCode()
    {
        return ContainerId.GetHashCode();
    }
}
