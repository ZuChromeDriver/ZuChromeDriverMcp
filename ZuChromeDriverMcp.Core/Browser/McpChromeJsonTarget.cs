using System.Text.Json;
using System.Text.Json.Serialization;
using Zu.ChromeDevTools;

namespace ZuChromeDriverMcp.Core.Browser;

/// <summary>
/// Extended /json list entry (includes <c>url</c> not mapped on <see cref="ChromeSessionInfo"/>).
/// </summary>
public sealed class McpChromeJsonTarget
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    public static async Task<IReadOnlyList<McpChromeJsonTarget>> FetchPageTargetsAsync(int port, CancellationToken cancellationToken)
    {
        var uri = new UriBuilder { Scheme = "http", Host = "127.0.0.1", Port = port, Path = "/json" }.Uri;
        using var client = new HttpClient();
        var json = await client.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
        var targets = JsonSerializer.Deserialize<McpChromeJsonTarget[]>(json, ChromeDevToolsJsonSerializerOptions.Instance);
        if (targets == null || targets.Length == 0)
            return Array.Empty<McpChromeJsonTarget>();

        return targets
            .Where(t => string.Equals(t.Type, "page", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
