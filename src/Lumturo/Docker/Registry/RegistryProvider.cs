using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Lumturo.Docker.Registry;

public interface IDockerRegistryProvider
{
    Task<bool> CheckIsLatestAsync(string imageName, string imageDigest, CancellationToken cancellationToken = default);
}

public class DockerRegistryProvider : IDockerRegistryProvider, IDisposable
{
    private readonly Dictionary<string, AuthToken> _tokens = [];
    private readonly HttpClient _authClient = new() { BaseAddress = new Uri("https://auth.docker.io/") };
    private readonly HttpClient _indexClient;
    private bool _disposed;

    public DockerRegistryProvider()
    {
        _indexClient = new() { BaseAddress = new Uri("https://index.docker.io/v2/") };
        _indexClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.distribution.manifest.v2+json"));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<bool> CheckIsLatestAsync(string imageName, string imageDigest, CancellationToken cancellationToken = default)
    {
        var token = (await AuthAsync(imageName, cancellationToken)).Token;
        _indexClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _indexClient.GetAsync($"library/{imageName}/manifests/latest", cancellationToken);
        response.EnsureSuccessStatusCode();
        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

        var tagManifest = JsonSerializer.Deserialize<TagManifest>(rawContent)
            ?? throw new InvalidOperationException("Docker registry response is empty");

        if (tagManifest.SchemaVersion != 2)
        {
            throw new InvalidOperationException($"Unsupported schema version {tagManifest.SchemaVersion}");
        }

        switch (tagManifest.MediaType)
        {
            case "application/vnd.oci.image.index.v1+json":
                var imageIndex = JsonSerializer.Deserialize<ImageIndexV1>(rawContent)
                    ?? throw new InvalidOperationException("Docker registry response is empty");
                foreach (var imageIndexManifest in imageIndex.Manifests)
                {
                    if (imageDigest == imageIndexManifest.Digest)
                    {
                        return true;
                    }
                }
                return false;
            case "application/vnd.docker.distribution.manifest.v2+json":
                var manifest = JsonSerializer.Deserialize<DistributionManifestV2>(rawContent)
                    ?? throw new InvalidOperationException("Docker registry response is empty");
                return manifest.Config.Digest == imageDigest;
            default:
                throw new InvalidOperationException($"Unsupported media type {tagManifest.MediaType}");
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            _authClient.Dispose();
            _indexClient.Dispose();
        }
        _disposed = true;
    }

    private async Task<AuthToken> AuthAsync(string imageName, CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryGetValue(imageName, out var authToken) || authToken.Expired)
        {
            using var client = new HttpClient { BaseAddress = new Uri("https://auth.docker.io/") };
            authToken = await _authClient.GetFromJsonAsync<AuthToken>($"token?service=registry.docker.io&scope=repository:library/{imageName}:pull", cancellationToken)
                ?? throw new InvalidOperationException("Docker authentication response is empty");
            _tokens[imageName] = authToken;
        }
        return authToken;
    }
}
