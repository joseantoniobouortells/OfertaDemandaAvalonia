using System;

namespace OfertaDemanda.Shared.Settings;

public interface IAppConfigStore
{
    string SettingsFilePath { get; }
    UserSettings Load();
    void Save(UserSettings settings);
    event EventHandler<UserSettings>? SettingsChanged;
}
