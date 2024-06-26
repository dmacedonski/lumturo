namespace Lumturo;

public enum ImageStatus { New, UpToDate, OutOfDate }

public class ScannerCacheItem
{
    public required string[] RepoTags { get; init; }
    public ImageStatus Status { get; set; } = ImageStatus.New;
}
