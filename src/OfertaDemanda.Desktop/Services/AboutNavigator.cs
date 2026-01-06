using System;

namespace OfertaDemanda.Desktop.Services;

public sealed class AboutNavigator : IAboutNavigator
{
    public event EventHandler? Requested;

    public void ShowAbout()
    {
        Requested?.Invoke(this, EventArgs.Empty);
    }
}
