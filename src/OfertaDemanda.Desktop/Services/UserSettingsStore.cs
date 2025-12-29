using System;
using System.IO;
using System.Text.Json;

namespace OfertaDemanda.Desktop.Services;

public sealed class UserSettingsStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public UserSettingsStore(string? customPath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(customPath) ? ResolveDefaultPath() : customPath!;
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public string SettingsFilePath => _filePath;

    public UserSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return UserSettings.CreateDefault();
            }

            var json = File.ReadAllText(_filePath);
            var payload = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
            return (payload ?? UserSettings.CreateDefault()).Sanitize();
        }
        catch
        {
            return UserSettings.CreateDefault();
        }
    }

    public void Save(UserSettings settings)
    {
        var sanitized = (settings ?? UserSettings.CreateDefault()).Sanitize();
        var json = JsonSerializer.Serialize(sanitized, JsonOptions);
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
}
