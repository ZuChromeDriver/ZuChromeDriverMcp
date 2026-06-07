using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZuChromeDriverMcp.Core.Configuration;

public static class McpHostSettingsStore
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string GetSettingsFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "settings.json");
    }

    public static McpHostSettingsSnapshot TryLoad()
    {
        var path = GetSettingsFilePath();
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<McpHostSettingsSnapshot>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(McpHostOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        Save(options.ToSettingsSnapshot());
    }

    public static void Save(McpHostSettingsSnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        File.WriteAllText(GetSettingsFilePath(), json);
    }
}
