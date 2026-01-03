using System;
using System.IO;
using System.Text.Json;
using Microsoft.Maui.Storage;
using OfertaDemanda.Shared.Settings;

namespace OfertaDemanda.Mobile.Services;

public sealed class MobileSettingsStore : IAppConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public MobileSettingsStore()
    {
        var directory = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "settings.json");
    }

    public string SettingsFilePath => _filePath;

    public event EventHandler<UserSettings>? SettingsChanged;

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
        SettingsChanged?.Invoke(this, sanitized);
    }
}
