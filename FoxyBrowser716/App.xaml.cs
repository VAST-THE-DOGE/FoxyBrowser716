using System.IO.Pipes;
using System.Windows;
using System.Windows.Threading;
using FoxyBrowser716.ErrorHandling;
using static FoxyBrowser716.InstallationManager;

namespace FoxyBrowser716;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "FoxyBrowser716_Mutex";
    
    protected override void OnStartup(StartupEventArgs e)
    {
        if (ProcessCommandLineArguments(e.Args))
        {
            Current.Shutdown();
            return;
        }
        
        _mutex = new Mutex(true, MutexName, out var isNewInstance);

        if (!isNewInstance)
        {
            SendMessage($"NewWindow|{e.Args.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))}");
            Current.Shutdown();
            return;
        }

        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        Current.Exit += OnApplicationExit;

        if (!IsBrowserInstalled())
        {
            if (MessageBox.Show(
                $"Would you like to register {GetApplicationName()} with Windows?\nThis allows you to interact with it like any other app and it allows you set it as your default browser.\n(note: administrative permissions required for this)",
                "Register Browser?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RegisterBrowser();
            }
        }

        ServerManager.RunServer(e);
    }

    private bool ProcessCommandLineArguments(string[] args)
    {
        if (args.Length == 0) return false;
        
        foreach (var arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--register":
                    InstallationManager.RegisterBrowser();
                    return true;
                    
                case "--uninstall":
                    throw new NotImplementedException();
                    return true;
                    
                case "--reinstall":
                    InstallationManager.RegisterBrowser();
                    return true;
                    
                case "--search":
                    return false;
                
                case "--create-shortcuts":
                    CreateAppShortcuts();
                    return true;
            }
        }
        
        return false;
    }

    private void OnApplicationExit(object sender, ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;
    }
    
    private void CreateAppShortcuts()
    {
        try
        {
            var appName = InstallationManager.GetApplicationName();
            var exePath = InstallationManager.GetExecutablePath();
            
            var installationType = typeof(InstallationManager);
            var method = installationType.GetMethod("CreateShortcuts", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (method != null)
            {
                method.Invoke(null, new object[] { appName, exePath, true });
                MessageBox.Show($"Shortcuts for {appName} have been created successfully.", 
                    "Shortcuts Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating shortcuts: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var errorPopup = new ErrorPopup(e);
        errorPopup.ShowDialog();
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception ex) return;
        
        var errorPopup = new ErrorPopup(ex);
        errorPopup.ShowDialog();
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var errorPopup = new ErrorPopup(e);
        errorPopup.ShowDialog();
        e.SetObserved();
    }

    static void SendMessage(string message)
    {
        using var client = new NamedPipeClientStream(".", "FoxyBrowser716_Pipe", PipeDirection.Out);
        client.Connect(1000);
        using var writer = new System.IO.StreamWriter(client);
        writer.WriteLine(message);
        writer.Flush();
    }
}