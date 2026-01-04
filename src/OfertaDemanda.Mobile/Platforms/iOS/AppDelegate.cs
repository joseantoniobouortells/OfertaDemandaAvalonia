using System;
using Foundation;
using OfertaDemanda.Mobile.Services;
using UIKit;

namespace OfertaDemanda.Mobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool FinishedLaunching(UIApplication app, NSDictionary options)
	{
		try
		{
			return base.FinishedLaunching(app, options);
		}
		catch (Exception ex)
		{
			CrashReporter.Log(ex, "AppDelegate.FinishedLaunching");
			throw;
		}
	}
}
