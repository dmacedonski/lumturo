using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lumturo;

public enum ScanLevel { All, Used, Marked }

public class ScanLevelConverter : JsonConverter<ScanLevel>
{
    public override ScanLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? levelAsString = reader.GetString();
        return levelAsString switch
        {
            "All" => ScanLevel.All,
            "Used" => ScanLevel.Used,
            "Marked" => ScanLevel.Marked,
            _ => throw new InvalidOperationException($"Unsupported scan level {levelAsString}, use one of {String.Join(", ", Enum.GetNames(typeof(ScanLevel)))}"),
        };
    }

    public override void Write(Utf8JsonWriter writer, ScanLevel value, JsonSerializerOptions options)
    {
        string levelAsString = value switch
        {
            ScanLevel.All => "All",
            ScanLevel.Used => "Used",
            ScanLevel.Marked => "Marked",
            _ => throw new InvalidOperationException($"Unsupported scan level value {value}"),
        };
        writer.WriteStringValue(levelAsString);
    }
}

public class LumturoConfig
{
    [JsonConverter(typeof(ScanLevelConverter))]
    public ScanLevel ScanLevel { get; set; } = ScanLevel.All;
    public TimeSpan ScanPeriod { get; set; } = TimeSpan.FromHours(1);
}
