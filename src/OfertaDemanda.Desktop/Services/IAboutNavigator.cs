using System;

namespace OfertaDemanda.Desktop.Services;

public interface IAboutNavigator
{
    event EventHandler? Requested;
    void ShowAbout();
}
