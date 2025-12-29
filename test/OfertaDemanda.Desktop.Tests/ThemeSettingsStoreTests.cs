using System;
using System.IO;
using OfertaDemanda.Desktop.Services;

namespace OfertaDemanda.Desktop.Tests;

public sealed class ThemeSettingsStoreTests : IDisposable
{
    private readonly string _tempDirectory;

    public ThemeSettingsStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"oferta-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Load_ReturnsSystem_WhenFileMissing()
    {
        var path = Path.Combine(_tempDirectory, "settings.json");
        var store = new ThemeSettingsStore(path);

        var mode = store.Load();

        Assert.Equal(ThemeMode.System, mode);
    }

    [Fact]
    public void Save_And_Load_Roundtrip()
    {
        var path = Path.Combine(_tempDirectory, "settings.json");
        var store = new ThemeSettingsStore(path);

        store.Save(ThemeMode.Dark);
        var mode = store.Load();

        Assert.Equal(ThemeMode.Dark, mode);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore clean-up failures.
        }
    }
}
