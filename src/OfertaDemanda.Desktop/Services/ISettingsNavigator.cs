using System;

namespace OfertaDemanda.Desktop.Services;

public interface ISettingsNavigator
{
    event EventHandler? Requested;
    void ShowSettings();
}
