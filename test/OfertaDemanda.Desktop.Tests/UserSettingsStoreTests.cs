using System;
using System.IO;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Shared.Settings;

namespace OfertaDemanda.Desktop.Tests;

public sealed class UserSettingsStoreTests : IDisposable
{
    private readonly string _tempDirectory;

    public UserSettingsStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"oferta-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileMissing()
    {
        var path = Path.Combine(_tempDirectory, "settings.json");
        var store = new UserSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(ThemeMode.System, settings.Theme);
        Assert.Equal(3, settings.IsoBenefit.Firms.Count);
    }

    [Fact]
    public void Save_And_Load_Roundtrip()
    {
        var path = Path.Combine(_tempDirectory, "settings.json");
        var store = new UserSettingsStore(path);
        var original = UserSettings.CreateDefault() with
        {
            Theme = ThemeMode.Dark,
            IsoBenefit = UserSettings.CreateDefault().IsoBenefit with
            {
                DemandExpression = "150 - q",
                Firms = new[]
                {
                    new IsoBenefitFirmSetting("Test", "100 + 4q")
                }
            }
        };

        store.Save(original);
        var loaded = store.Load();

        Assert.Equal(ThemeMode.Dark, loaded.Theme);
        Assert.Single(loaded.IsoBenefit.Firms);
        Assert.Equal("Test", loaded.IsoBenefit.Firms[0].Name);
        Assert.Equal("150 - q", loaded.IsoBenefit.DemandExpression);
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
