using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Services;

public sealed class AppConfigStore
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public AppConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var createdConfig = new AppConfig();
            Save(path, createdConfig);
            return createdConfig;
        }

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, _serializerOptions) ?? new AppConfig();
        return config;
    }

    public void Save(string path, AppConfig config)
    {
        config.LastSavedUtc = DateTimeOffset.UtcNow;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(config, _serializerOptions));
    }
}
