using System;

namespace OfertaDemanda.Desktop.Services;

public sealed class SettingsNavigator : ISettingsNavigator
{
    public event EventHandler? Requested;

    public void ShowSettings()
    {
        Requested?.Invoke(this, EventArgs.Empty);
    }
}
