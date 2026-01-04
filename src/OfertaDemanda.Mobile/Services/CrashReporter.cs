using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Storage;

namespace OfertaDemanda.Mobile.Services;

public static class CrashReporter
{
    private const int MaxFirstChanceLogs = 50;
    private static int _firstChanceCount;

    public static void HookEarly()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log(args.ExceptionObject as Exception, "AppDomain.UnhandledException");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved();
        };

        AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
        {
            if (ShouldLogFirstChance(args.Exception))
            {
                Log(args.Exception, "AppDomain.FirstChanceException");
            }
        };
    }

    public static void Log(Exception? exception, string source)
    {
        if (exception == null)
        {
            return;
        }

        var message = $"{source}: {exception}";
        Debug.WriteLine(message);
        Console.WriteLine(message);
        TryAppendLog(message);
    }

    private static bool ShouldLogFirstChance(Exception exception)
    {
        if (Interlocked.Increment(ref _firstChanceCount) > MaxFirstChanceLogs)
        {
            return false;
        }

        var source = exception.Source ?? string.Empty;
        if (source.Contains("OfertaDemanda", StringComparison.Ordinal))
        {
            return true;
        }

        var stackTrace = exception.StackTrace ?? string.Empty;
        return stackTrace.Contains("OfertaDemanda", StringComparison.Ordinal);
    }

    private static void TryAppendLog(string message)
    {
        try
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "crash-log.txt");
            File.AppendAllText(path, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Ignore logging failures.
        }
    }
}
