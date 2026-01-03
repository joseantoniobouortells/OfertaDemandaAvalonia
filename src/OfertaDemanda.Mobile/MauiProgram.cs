using LiveChartsCore.SkiaSharpView.Maui;
using OfertaDemanda.Mobile.Services;
using OfertaDemanda.Mobile.ViewModels;
using OfertaDemanda.Mobile.Views;
using OfertaDemanda.Shared.Settings;

namespace OfertaDemanda.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseLiveCharts()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<IAppConfigStore, MobileSettingsStore>();
		builder.Services.AddSingleton<UserSettingsService>();
		builder.Services.AddSingleton<LocalizationService>();
		builder.Services.AddSingleton<ThemeService>();
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<SettingsViewModel>();
		builder.Services.AddSingleton<AboutViewModel>();
		builder.Services.AddSingleton<MainTabbedPage>();

		return builder.Build();
	}
}
