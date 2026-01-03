using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;
using OfertaDemanda.Mobile.Services;

namespace OfertaDemanda.Mobile.ViewModels;

public sealed partial class AboutViewModel : ObservableObject
{
    public AboutViewModel(LocalizationService localization)
    {
        Localization = localization;
        AppName = AppInfo.Name;
        Version = AppInfo.VersionString;
        Platform = DeviceInfo.Platform.ToString();
        Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
        Dotnet = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        Avalonia = "MAUI";
        BuildDate = ReadAssemblyMetadata("BuildDate") ?? "unknown";
        Commit = ReadAssemblyMetadata("Commit") ?? "unknown";
    }

    public LocalizationService Localization { get; }

    public string AppName { get; }
    public string Version { get; }
    public string Platform { get; }
    public string Architecture { get; }
    public string Dotnet { get; }
    public string Avalonia { get; }
    public string BuildDate { get; }
    public string Commit { get; }

    private static string? ReadAssemblyMetadata(string key)
    {
        var asm = Assembly.GetExecutingAssembly();
        return asm.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value;
    }
}
