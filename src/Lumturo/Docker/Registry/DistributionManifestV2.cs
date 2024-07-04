using System.Text.Json.Serialization;

namespace Lumturo.Docker.Registry;

public class Config
{
    [JsonPropertyName("mediaType")]
    public required string MediaType { get; init; }

    [JsonPropertyName("size")]
    public required long Size { get; init; }

    [JsonPropertyName("digest")]
    public required string Digest { get; init; }
}

public class Layer
{
    [JsonPropertyName("mediaType")]
    public required string MediaType { get; init; }

    [JsonPropertyName("size")]
    public required long Size { get; init; }

    [JsonPropertyName("digest")]
    public required string Digest { get; init; }
}

public class DistributionManifestV2 : TagManifest
{
    [JsonPropertyName("config")]
    public required Config Config { get; init; }

    [JsonPropertyName("layers")]
    public required Layer[] Layers { get; init; }
}
