using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;

namespace OfertaDemanda.Desktop.ViewModels;

public sealed partial class AboutViewModel : ObservableObject
{
    public string AuthorName { get; } = "José Antonio Bou Ortells";
    public string AuthorRole { get; } = "Ingeniero de software · Estudiante del Grado en Economía (UNED)";
    public string PurposeText { get; } =
        "OfertaDemanda es una herramienta educativa de microeconomía. " +
        "Permite explorar oferta y demanda, monopolio, elasticidades, costes e isobeneficio con gráficos interactivos.";

    public string AppName { get; }
    public string AppVersion { get; }
    public string BuildDate { get; }
    public string CommitHash { get; }
    public string OsDescription { get; }
    public string DotnetVersion { get; }
    public string AvaloniaVersion { get; }
    public string Architecture { get; }
    public string RepositoryUrl { get; }
    public bool HasRepositoryUrl => !string.IsNullOrWhiteSpace(RepositoryUrl);
    public bool HasNoRepositoryUrl => !HasRepositoryUrl;

    public IRelayCommand OpenRepositoryCommand { get; }

    public AboutViewModel()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(AboutViewModel).Assembly;
        AppName = assembly.GetName().Name ?? "OfertaDemandaAvalonia";
        AppVersion = ResolveVersion(assembly);
        BuildDate = ResolveBuildDate(assembly);
        CommitHash = ResolveCommitHash(assembly);
        RepositoryUrl = ResolveRepositoryUrl(assembly);
        OsDescription = RuntimeInformation.OSDescription;
        DotnetVersion = RuntimeInformation.FrameworkDescription;
        AvaloniaVersion = typeof(Application).Assembly.GetName().Version?.ToString() ?? "—";
        Architecture = $"{RuntimeInformation.OSArchitecture}/{RuntimeInformation.ProcessArchitecture}";
        OpenRepositoryCommand = new RelayCommand(OpenRepository, () => HasRepositoryUrl);
    }

    private void OpenRepository()
    {
        if (!HasRepositoryUrl)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = RepositoryUrl,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private static string ResolveVersion(Assembly assembly)
    {
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var trimmed = info.Split('+', 2, StringSplitOptions.RemoveEmptyEntries)[0];
            return trimmed;
        }

        return assembly.GetName().Version?.ToString() ?? "—";
    }

    private static string ResolveBuildDate(Assembly assembly)
    {
        var value = GetAssemblyMetadata(assembly, "BuildDate");
        if (DateTimeOffset.TryParse(value, out var parsed))
        {
            return parsed.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'");
        }

        return "—";
    }

    private static string ResolveCommitHash(Assembly assembly)
    {
        var value = GetAssemblyMetadata(assembly, "Commit");
        if (string.IsNullOrWhiteSpace(value) || value.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return "—";
        }

        return value.Length > 12 ? value[..12] : value;
    }

    private static string ResolveRepositoryUrl(Assembly assembly)
    {
        return GetAssemblyMetadata(assembly, "RepositoryUrl");
    }

    private static string GetAssemblyMetadata(Assembly assembly, string key)
    {
        foreach (var metadata in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (string.Equals(metadata.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return metadata.Value ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
