using System.Text.Json.Serialization;

namespace Lumturo.Docker.Registry;

public class AuthToken
{
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; init; }

    [JsonPropertyName("issued_at")]
    public required DateTime IssuedAt { get; init; }

    public bool Expired => IssuedAt.AddSeconds(ExpiresIn).CompareTo(DateTime.UtcNow) <= 0;
}
