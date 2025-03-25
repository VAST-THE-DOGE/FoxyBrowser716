using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace FoxyBrowser716;

public static class InstallationManager
{
	public static bool IsBrowserInstalled()
	{
		var keyPath = $@"Software\Clients\StartMenuInternet\{GetApplicationName()}";
		return Registry.CurrentUser.OpenSubKey(keyPath) != null;
	}

	public static void RegisterBrowser()
    {
        var appName = GetApplicationName();
        var startMenuKeyPath = $@"Software\Clients\StartMenuInternet\{appName}";
        // Create the StartMenuInternet registration key.
        using (var key = Registry.CurrentUser.CreateSubKey(startMenuKeyPath))
        {
            key.SetValue("", appName);
            using (var capabilities = key.CreateSubKey("Capabilities"))
            {
                capabilities.SetValue("ApplicationName", appName);
                capabilities.SetValue("ApplicationDescription", "A simple, yet powerful browser. Built around C# WPF and WebViewV2. Developed by Vast the Doge (https://github.com/VAST-THE-DOGE) with some help from FoxyGuy716.");
                
                // Set up URL associations.
                using (var urlAssociations = capabilities.CreateSubKey("URLAssociations"))
                {
                    urlAssociations.SetValue("http", $"{appName}HTML");
                    urlAssociations.SetValue("https", $"{appName}HTML");
                }
            }
        }

        // Register the open command.
        var commandKeyPath = $@"{startMenuKeyPath}\shell\open\command";
        using (var commandKey = Registry.CurrentUser.CreateSubKey(commandKeyPath))
        {
            commandKey.SetValue("", $"\"{GetExecutablePath()}\" \"%1\"");
        }

        // Register in RegisteredApplications so that it shows up in Default Programs.
        using (var regApps = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
        {
            regApps.SetValue(appName, $@"Software\Clients\StartMenuInternet\{appName}\Capabilities");
        }

        // Create a ProgID for URL associations under HKCU\Software\Classes.
        var progId = $"{appName}HTML";
        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
        {
            progIdKey.SetValue("", $"{appName} Document");
            using (var defaultIcon = progIdKey.CreateSubKey("DefaultIcon"))
            {
                defaultIcon.SetValue("", $"\"{GetExecutablePath()}\",0");
            }
            using (var shell = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                shell.SetValue("", $"\"{GetExecutablePath()}\" \"%1\"");
            }
        }
        
        using (RegistryKey appPaths = Registry.CurrentUser.CreateSubKey(
	               $@"Software\Microsoft\Windows\CurrentVersion\App Paths\{GetApplicationName()}.exe"))
        {
	        var exePath = GetExecutablePath();
	        appPaths.SetValue("", exePath);
	        // This makes sure the directory is in the PATH when your app runs from the command line.
	        appPaths.SetValue("Path", Path.GetDirectoryName(exePath));
        }
    }

	public static string GetExecutablePath()
	{
		return Process.GetCurrentProcess().MainModule.FileName;
	}

	public static string GetApplicationName()
	{
		return Path.GetFileNameWithoutExtension(GetExecutablePath());
	}
}