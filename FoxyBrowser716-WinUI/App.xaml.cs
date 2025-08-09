
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataManagement;
using Microsoft.Windows.AppLifecycle;

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
        // Get the current app instance
        var currentInstance = AppInstance.GetCurrent();
            
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