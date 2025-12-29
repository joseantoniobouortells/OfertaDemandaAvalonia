using System;
using System.IO;
using System.Text.Json;

namespace OfertaDemanda.Desktop.Services;

public sealed class ThemeSettingsStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public ThemeSettingsStore(string? customPath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(customPath) ? ResolveDefaultPath() : customPath!;
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public string SettingsFilePath => _filePath;

    public ThemeMode Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return ThemeMode.System;
            }

            var json = File.ReadAllText(_filePath);
            var payload = JsonSerializer.Deserialize<ThemeSettingsPayload>(json, JsonOptions);
            return payload?.Theme ?? ThemeMode.System;
        }
        catch
        {
            return ThemeMode.System;
        }
    }

    public void Save(ThemeMode mode)
    {
        var payload = new ThemeSettingsPayload { Theme = mode };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static string ResolveDefaultPath()
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(basePath))
        {
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        if (string.IsNullOrWhiteSpace(basePath))
        {
            basePath = AppContext.BaseDirectory;
        }

        var directory = Path.Combine(basePath, "OfertaDemandaAvalonia");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "settings.json");
    }

    private sealed record ThemeSettingsPayload
    {
        public ThemeMode Theme { get; init; } = ThemeMode.System;
    }
}
