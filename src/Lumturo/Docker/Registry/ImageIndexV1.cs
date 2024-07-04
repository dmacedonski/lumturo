using System.Text.Json.Serialization;

namespace Lumturo.Docker.Registry;

public class Platform
{
    [JsonPropertyName("architecture")]
    public required string Architecture { get; init; }

    [JsonPropertyName("os")]
    public required string Os { get; init; }

    [JsonPropertyName("variant")]
    public string? Variant { get; init; }
}

public class Manifest
{
    [JsonPropertyName("annotations")]
    public required IDictionary<string, string> Annotations { get; init; }

    [JsonPropertyName("digest")]
    public required string Digest { get; init; }

    [JsonPropertyName("mediaType")]
    public required string MediaType { get; init; }

    [JsonPropertyName("platform")]
    public required Platform Platform { get; init; }

    [JsonPropertyName("size")]
    public required long Size { get; init; }
}

public class ImageIndexV1 : TagManifest
{
    [JsonPropertyName("manifests")]
    public required Manifest[] Manifests { get; init; }
}