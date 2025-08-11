﻿
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.ApplicationModel.Activation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataManagement;
using Microsoft.Windows.AppLifecycle;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace FoxyBrowser716_WinUI;

public partial class App : Application
{
    private const string AppKey =
#if DEBUG
        "FoxyBrowser716-WinUI-Debug";
#else
            "FoxyBrowser716-WinUI-Prod";
#endif
            

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        //need this to be able to compete with unoptimized games to prevent small freezes in some sites like youtube music:
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        
        // other performance optimizations:
        // GCSettings.LatencyMode = GCLatencyMode.Batch; // Lag spikes?
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        ThreadPool.SetMinThreads(Environment.ProcessorCount * 4, Environment.ProcessorCount * 2);
        ThreadPool.SetMaxThreads(Environment.ProcessorCount * 8, Environment.ProcessorCount * 4);
        Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
        
        Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);
        
        // note that webview2 has its own similar optimizations in WebviewTab.cs.

        // Get the current app instance
        var currentInstance = AppInstance.GetCurrent();

#if !DEBUG
        this.UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
#endif

        
        // Check if this is the first instance
        var mainInstance = AppInstance.FindOrRegisterForKey(AppKey);
            
        if (!mainInstance.IsCurrent)
        {
            var activationArgs = currentInstance.GetActivatedEventArgs();
            mainInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
                
            System.Environment.Exit(0);
            return;
        }

        // This is the main instance, set up activation handling
        currentInstance.Activated += OnActivated;
            
        var e = currentInstance.GetActivatedEventArgs();
            
        _ = HandleActivationArgs(e, true);
    }

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception is Exception ex)
            Debug.WriteLine(ex);
        //TODO
        // throw new NotImplementedException();
    }

    private void CurrentDomainOnUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            Debug.WriteLine(ex);
        //TODO
        // throw new NotImplementedException();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.Exception is Exception ex)
            Debug.WriteLine(ex);
        //TODO
        // throw new NotImplementedException();
        e.Handled = true;
    }

    private void OnActivated(object? sender, AppActivationArguments e)
    {
        _ = HandleActivationArgs(e, false);
    }
        
    private async Task HandleActivationArgs(AppActivationArguments args, bool isFirst)
    {
        switch (args.Kind)
        {
            case ExtendedActivationKind.Launch:
                if (args.Data is ILaunchActivatedEventArgs launchArgs)
                {
                    var arguments = launchArgs.Arguments;
                    await AppServer.HandleLaunchEvent(arguments?.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? [], isFirst);
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
                    await AppServer.HandleLaunchEvent(arguments?.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? [], isFirst);
                }
                break;
        }
    }
}