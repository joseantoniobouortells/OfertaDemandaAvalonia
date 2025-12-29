namespace OfertaDemanda.Desktop.Services;

public sealed class UserSettingsService
{
    private readonly UserSettingsStore _store;

    public UserSettingsService(UserSettingsStore store)
    {
        _store = store;
        Settings = _store.Load();
    }

    public UserSettings Settings { get; private set; }

    public string SettingsFilePath => _store.SettingsFilePath;

    public void Update(UserSettings newSettings)
    {
        Settings = newSettings.Sanitize();
        _store.Save(Settings);
    }
}
