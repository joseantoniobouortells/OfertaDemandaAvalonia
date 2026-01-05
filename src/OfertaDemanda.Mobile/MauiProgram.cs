using System;
using LiveChartsCore.SkiaSharpView.Maui;
using OfertaDemanda.Mobile.Services;
using OfertaDemanda.Mobile.ViewModels;
using OfertaDemanda.Mobile.Views;
using OfertaDemanda.Shared.Settings;
using OfertaDemanda.Shared.Math;
using SkiaSharp.Views.Maui.Handlers;

namespace OfertaDemanda.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		CrashReporter.HookEarly();

		try
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
			builder.ConfigureMauiHandlers(handlers =>
			{
				var cpuRenderMode = Type.GetType("LiveChartsCore.SkiaSharpView.Maui.Rendering.CPURenderMode, LiveChartsCore.SkiaSharpView.Maui");
				if (cpuRenderMode != null)
				{
					handlers.AddHandler(cpuRenderMode, typeof(SKCanvasViewHandler));
				}

				var gpuRenderMode = Type.GetType("LiveChartsCore.SkiaSharpView.Maui.Rendering.GPURenderMode, LiveChartsCore.SkiaSharpView.Maui");
				if (gpuRenderMode != null)
				{
					handlers.AddHandler(gpuRenderMode, typeof(SKGLViewHandler));
				}
			});

			builder.Services.AddSingleton<IAppConfigStore, MobileSettingsStore>();
			builder.Services.AddSingleton<UserSettingsService>();
			builder.Services.AddSingleton<LocalizationService>();
			builder.Services.AddSingleton<ThemeService>();
			builder.Services.AddSingleton<IMathFormulaRenderer, CSharpMathFormulaRenderer>();
			builder.Services.AddSingleton<MainViewModel>();
			builder.Services.AddSingleton<SettingsViewModel>();
			builder.Services.AddSingleton<AboutViewModel>();

			return builder.Build();
		}
		catch (Exception ex)
		{
			CrashReporter.Log(ex, "MauiProgram.CreateMauiApp");
			throw;
		}
	}
}
