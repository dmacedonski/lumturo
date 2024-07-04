using System.Text.Json.Serialization;

namespace Lumturo.Docker.Registry;

public class TagManifest
{
    [JsonPropertyName("schemaVersion")]
    public required uint SchemaVersion { get; init; }

    [JsonPropertyName("mediaType")]
    public required string MediaType { get; init; }
}
