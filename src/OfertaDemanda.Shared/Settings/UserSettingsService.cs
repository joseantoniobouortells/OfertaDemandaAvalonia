namespace OfertaDemanda.Shared.Settings;

public sealed class UserSettingsService
{
    private readonly IAppConfigStore _store;

    public UserSettingsService(IAppConfigStore store)
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
