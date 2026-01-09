using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using FoxyBrowser716.Controls.MainWindow;
using FoxyBrowser716.DataManagement;
using FoxyBrowser716.ErrorHandeler;
using Microsoft.Windows.AppLifecycle;
using static System.Diagnostics.Process;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace FoxyBrowser716;

public partial class App : Application
{
    private const string AppKey =
#if DEBUG
        "FoxyBrowser716-Debug";
#else
            "FoxyBrowser716-Prod";
#endif

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            // performance optimizations:
            // compiles JIT code for the startup profile which is reused after the first launch
            var profileRoot = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            ProfileOptimization.SetProfileRoot(profileRoot);
            ProfileOptimization.StartProfile("Startup.profile");
            
            // Get the current app instance
            var currentInstance = AppInstance.GetCurrent();
        
            // Check if this is the first instance
            var mainInstance = AppInstance.FindOrRegisterForKey(AppKey);
            
            if (!mainInstance.IsCurrent)
            {
                var activationArgs = currentInstance.GetActivatedEventArgs();
                try
                {
                    await mainInstance.RedirectActivationToAsync(activationArgs).AsTask();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RedirectActivationToAsync failed: {ex}"); 
                    FoxyLogger.AddError(ex);
                }

                Environment.Exit(0);
                return;
            }
        
            FoxyLogger.LoadLog();
            
            this.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnFirstChanceException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            
            // performance optimizations:
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            CoreApplication.EnablePrelaunch(true);
            _ = Task.Run(() =>
            {
                try
                {
                    GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
                    //ThreadPool.SetMinThreads(Environment.ProcessorCount * 4, Environment.ProcessorCount * 2);
                    //ThreadPool.SetMaxThreads(Environment.ProcessorCount * 8, Environment.ProcessorCount * 4);
                }
                catch { /* ignore if fails */ }
            });
            
            // note that webview2 has its own similar optimizations in WebviewTab.cs.

            // This is the main instance, set up activation handling
            currentInstance.Activated += OnActivated;
            
            var e = currentInstance.GetActivatedEventArgs();
            
            await HandleActivationArgs(e, true);
        }
        catch (Exception e)
        {
            FoxyLogger.AddCritical($"Final Catch App Error: {e.Message}", e.StackTrace);
            
            RequestRestartAfterClose();
        }
    }

    private void CurrentDomainOnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        if (e.Exception is Exception ex)
            FoxyLogger.AddCritical($"Uncaught App Error: {ex.Message}", ex.StackTrace);
        else
            FoxyLogger.AddCritical("Uncaught App Error: Unknown", "No error details available.");
    }

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception is Exception ex)
            FoxyLogger.AddCritical($"Uncaught App Error: {ex.Message}", ex.StackTrace);
        else
            FoxyLogger.AddCritical("Uncaught App Error: Unknown", "No error details available.");
        
        e.SetObserved();
    }

    private void CurrentDomainOnUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            FoxyLogger.AddCritical($"Uncaught App Error: {ex.Message}", ex.StackTrace);
        else
            FoxyLogger.AddCritical("Uncaught App Error: Unknown", "No error details available.");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.Exception is Exception ex)
            FoxyLogger.AddCritical($"Uncaught App Error: {ex.Message}", ex.StackTrace);
        else
            FoxyLogger.AddCritical("Uncaught App Error: Unknown", "No error details available.");

        e.Handled = true;
    }

    private async void OnActivated(object? sender, AppActivationArguments e)
    {
        await HandleActivationArgs(e, false);
    }
        
    private async Task HandleActivationArgs(AppActivationArguments args, bool isFirst)
    {
        switch (args.Kind)
        {
            case ExtendedActivationKind.StartupTask:
                if (args.Data is IStartupTaskActivatedEventArgs startupArgs)
                {
                    await AppServer.HandleLaunchEvent([], isFirst, true);
                }
                break;
            case ExtendedActivationKind.Launch:
                if (args.Data is ILaunchActivatedEventArgs launchArgs)
                {
                    var arguments = launchArgs.Arguments;
                    await AppServer.HandleLaunchEvent(
                        arguments?
                            .Split(" ")
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToArray() ?? [], isFirst
                        );
                }
                break;
            case ExtendedActivationKind.Protocol:
                if (args.Data is IProtocolActivatedEventArgs protocolArgs)
                {
                    var uri = protocolArgs.Uri;
                    await AppServer.HandleLaunchEvent([uri.ToString()], isFirst);
                }
                break;
            case ExtendedActivationKind.File:
                if (args.Data is IFileActivatedEventArgs fileArgs)
                {
                    var uris = fileArgs.Files.Select(f => f.Path).ToArray();
                    await AppServer.HandleLaunchEvent(uris, isFirst);
                }
                break;
            case ExtendedActivationKind.CommandLineLaunch:
                if (args.Data is ICommandLineActivatedEventArgs commandArgs)
                {
                    var arguments = commandArgs.Operation.Arguments;
                    await AppServer.HandleLaunchEvent(
                        arguments?
                            .Split(" ")
                            .Skip(1 /*command name, such as FoxyBrowser716.exe or FoxyBrowser716*/)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToArray() ?? [], isFirst
                        );
                }
                break;
        }
    }

    private void RequestRestartAfterClose()
    {
        try
        {
            var currentPid = Environment.ProcessId;
            var appUserModelId = Windows.ApplicationModel.AppInfo.Current.AppUserModelId;

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-WindowStyle Hidden -Command \"Wait-Process -Id {currentPid}; Start-Process shell:AppsFolder\\{appUserModelId}!App\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(psi);
            Environment.Exit(1);
        }
        catch (Exception e)
        {
            FoxyLogger.AddError(e);
        }
    }
}