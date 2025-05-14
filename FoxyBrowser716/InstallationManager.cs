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
        var exePath = GetExecutablePath();
        const string appDescription = "A simple, yet powerful browser. Built around C# WPF and WebViewV2. Developed by Vast the Doge (https://github.com/VAST-THE-DOGE) with some help from FoxyGuy716.";
        
        // Register as browser
        RegisterAsBrowser(appName, exePath, appDescription);
        
        // Register as app
        RegisterAsApp(appName, exePath, appDescription);
        
        // Register file associations for PDFs and other common web file types
        RegisterFileAssociations(appName, exePath);
    }

    private static void RegisterAsBrowser(string appName, string exePath, string appDescription)
    {
        // Register as Browser (StartMenuInternet)
        var startMenuKeyPath = $@"Software\Clients\StartMenuInternet\{appName}";
        using (var key = Registry.CurrentUser.CreateSubKey(startMenuKeyPath))
        {
            key.SetValue("", appName);
            using (var capabilities = key.CreateSubKey("Capabilities"))
            {
                capabilities.SetValue("ApplicationName", appName);
                capabilities.SetValue("ApplicationDescription", appDescription);
                
                using (var urlAssociations = capabilities.CreateSubKey("URLAssociations"))
                {
                    urlAssociations.SetValue("http", $"{appName}HTML");
                    urlAssociations.SetValue("https", $"{appName}HTML");
                    urlAssociations.SetValue("ftp", $"{appName}HTML");
                }
                
                // Add file associations within capabilities
                using (var fileAssociations = capabilities.CreateSubKey("FileAssociations"))
                {
                    fileAssociations.SetValue(".htm", $"{appName}HTML");
                    fileAssociations.SetValue(".html", $"{appName}HTML");
                    fileAssociations.SetValue(".pdf", $"{appName}PDF");
                    fileAssociations.SetValue(".svg", $"{appName}SVG");
                    fileAssociations.SetValue(".xhtml", $"{appName}HTML");
                    fileAssociations.SetValue(".xml", $"{appName}XML");
                    fileAssociations.SetValue(".webp", $"{appName}WEBP");
                }
            }
            
            // Add DefaultIcon
            using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
            {
                defaultIcon.SetValue("", $"\"{exePath}\",0");
            }
        }

        // Set up shell open command
        var commandKeyPath = $@"{startMenuKeyPath}\shell\open\command";
        using (var commandKey = Registry.CurrentUser.CreateSubKey(commandKeyPath))
        {
            commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
        }

        // Register with Windows
        using (var regApps = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
        {
            regApps.SetValue(appName, $@"Software\Clients\StartMenuInternet\{appName}\Capabilities");
        }

        // Create HTML ProgID
        var progId = $"{appName}HTML";
        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
        {
            progIdKey.SetValue("", $"{appName} HTML Document");
            progIdKey.SetValue("FriendlyTypeName", $"{appName} HTML Document");
            
            using (var defaultIcon = progIdKey.CreateSubKey("DefaultIcon"))
            {
                defaultIcon.SetValue("", $"\"{exePath}\",0");
            }
            
            using (var shell = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                shell.SetValue("", $"\"{exePath}\" \"%1\"");
            }
        }
    }

    private static void RegisterAsApp(string appName, string exePath, string appDescription)
    {
        // Register in App Paths
        using (var appPaths = Registry.CurrentUser.CreateSubKey(
               $@"Software\Microsoft\Windows\CurrentVersion\App Paths\{appName}.exe"))
        {
            appPaths.SetValue("", exePath);
            appPaths.SetValue("Path", Path.GetDirectoryName(exePath));
        }
        
        // Register in Applications
        using (var appKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\{appName}.exe"))
        {
            appKey.SetValue("FriendlyAppName", appName);
            appKey.SetValue("ApplicationDescription", appDescription);
            
            // Set up icon
            using (var defaultIcon = appKey.CreateSubKey("DefaultIcon"))
            {
                defaultIcon.SetValue("", $"\"{exePath}\",0");
            }
            
            // Set up command
            using (var command = appKey.CreateSubKey(@"shell\open\command"))
            {
                command.SetValue("", $"\"{exePath}\" \"%1\"");
            }
            
            // Set up supported file types in the Applications key
            using (var supportedTypes = appKey.CreateSubKey("SupportedTypes"))
            {
                supportedTypes.SetValue(".htm", "");
                supportedTypes.SetValue(".html", "");
                supportedTypes.SetValue(".pdf", "");
                supportedTypes.SetValue(".svg", "");
                supportedTypes.SetValue(".xhtml", "");
                supportedTypes.SetValue(".xml", "");
                supportedTypes.SetValue(".webp", "");
            }
        }
    }

    private static void RegisterFileAssociations(string appName, string exePath)
    {
        // Create PDF and other file type ProgIDs
        RegisterFileAssociation(appName, exePath, "PDF", ".pdf", "PDF Document");
        RegisterFileAssociation(appName, exePath, "SVG", ".svg", "SVG Image");
        RegisterFileAssociation(appName, exePath, "XML", ".xml", "XML Document");
        RegisterFileAssociation(appName, exePath, "WEBP", ".webp", "WebP Image");
        
        // Create file associations
        RegisterFileExtension(".pdf", $"{appName}PDF");
        RegisterFileExtension(".svg", $"{appName}SVG");
        RegisterFileExtension(".xml", $"{appName}XML");
        RegisterFileExtension(".webp", $"{appName}WEBP");
    }

    private static void RegisterFileAssociation(string appName, string exePath, string type, string extension, string description)
    {
        var progId = $"{appName}{type}";
        using var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
        progIdKey.SetValue("", $"{appName} {description}");
        progIdKey.SetValue("FriendlyTypeName", $"{appName} {description}");
            
        using (var defaultIcon = progIdKey.CreateSubKey("DefaultIcon"))
        {
            defaultIcon.SetValue("", $"\"{exePath}\",0");
        }
            
        using (var shell = progIdKey.CreateSubKey(@"shell\open\command"))
        {
            shell.SetValue("", $"\"{exePath}\" \"%1\"");
        }
    }

    private static void RegisterFileExtension(string extension, string progId)
    {
        using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}"))
        {
            extKey.SetValue("", progId);
            
            // Add OpenWithProgids entry to ensure it shows up in "Open with" dialog
            using (var openWithKey = extKey.CreateSubKey("OpenWithProgids"))
            {
                openWithKey.SetValue(progId, new byte[0], RegistryValueKind.None);
            }
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