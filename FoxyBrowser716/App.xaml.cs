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
            // Get the current app instance
            var currentInstance = AppInstance.GetCurrent();
        
            // Check if this is the first instance
            var mainInstance = AppInstance.FindOrRegisterForKey(AppKey);
            
            if (!mainInstance.IsCurrent)
            {
                var activationArgs = currentInstance.GetActivatedEventArgs();
                try
                {
                    // Await the redirect so it completes before we exit the process.
                    // If you cannot make OnLaunched async, block synchronously but intentionally:
                    await mainInstance.RedirectActivationToAsync(activationArgs).AsTask();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RedirectActivationToAsync failed: {ex}"); 
                    ErrorInfo.AddError(ex);
                }

                // After the redirect completes, exit.
                Environment.Exit(0);
                return;
            }
        
            // load errors:
            ErrorInfo.LoadLog();
            
// #if !DEBUG
            this.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnFirstChanceException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
// #endif
            
            // performance optimizations:
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            CoreApplication.EnablePrelaunch(true);
            _ = Task.Run(() =>
            {
                try
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                    ThreadPool.SetMinThreads(Environment.ProcessorCount * 4, Environment.ProcessorCount * 2);
                    ThreadPool.SetMaxThreads(Environment.ProcessorCount * 8, Environment.ProcessorCount * 4);
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
            Debug.WriteLine(e);
            ErrorInfo.AddError(e);
        }
    }

    private void CurrentDomainOnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        if (e.Exception is Exception ex)
        {
            Debug.WriteLine(ex);
            ErrorInfo.AddError(ex);
        }
    }

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception is Exception ex)
        {
            Debug.WriteLine(ex);
        }
        //TODO
        // throw new NotImplementedException();
        e.SetObserved();
    }

    private void CurrentDomainOnUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Debug.WriteLine(ex);
        }
        //TODO
        // throw new NotImplementedException();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.Exception is Exception ex)
        {
            Debug.WriteLine(ex);
        }
        //TODO
        // throw new NotImplementedException();
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
}