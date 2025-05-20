using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using System.Windows;

namespace FoxyBrowser716;

public static class InstallationManager
{
    private const string AppDescription = "A simple, yet powerful browser built with C# WPF and WebView2.";
    
    public static bool IsBrowserInstalled()
    {
        var appName = GetApplicationName();
        var executableName = Path.GetFileName(GetExecutablePath());
        
        var machineAppPath = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{executableName}");
        if (machineAppPath != null)
        {
            machineAppPath.Close();
            return true;
        }
        
        var userAppPath = Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\App Paths\{executableName}");
        if (userAppPath != null)
        {
            userAppPath.Close();
            return true;
        }
        
        var browserKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Clients\StartMenuInternet\{appName}");
        if (browserKey != null)
        {
            browserKey.Close();
            return true;
        }
        
        var userBrowserKey = Registry.CurrentUser.OpenSubKey($@"Software\Clients\StartMenuInternet\{appName}");
        if (userBrowserKey != null)
        {
            userBrowserKey.Close();
            return true;
        }
        
        return false;
    }
    
    public static void RegisterBrowser()
    {
        var appName = GetApplicationName();
        var exePath = GetExecutablePath();
        var executableName = Path.GetFileName(exePath);
        var appDir = Path.GetDirectoryName(exePath) ?? "";
        
        if (IsAdministrator())
        {
            RegisterWithAdminRights(appName, exePath, executableName, appDir);
        }
        else
        {
            RestartAsAdmin(exePath);
        }
    }
    
    private static void CreateShortcuts(string appName, string executablePath, bool createDesktopShortcut = true)
    {
        var startMenuPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs");
        
        CreateShortcutFile(appName, executablePath, startMenuPath);
        
        if (createDesktopShortcut)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CreateShortcutFile(appName, executablePath, desktopPath);
        }

        SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
    }

    private static void CreateShortcutFile(string appName, string targetPath, string directory)
    {
        var shortcutPath = Path.Combine(directory, $"{appName}.lnk");
        
        if (File.Exists(shortcutPath))
        {
            try { File.Delete(shortcutPath); } 
            catch { return; }
        }
        
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return;
        
        dynamic shell = Activator.CreateInstance(shellType);
        var shortcut = shell.CreateShortcut(shortcutPath);
        
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
        shortcut.Description = AppDescription;
        shortcut.IconLocation = $"{targetPath},0";
        shortcut.Save();
        
        Marshal.ReleaseComObject(shell);
    }
            

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
            
    private static void RegisterWithAdminRights(string appName, string exePath, string executableName, string appDir)
    {
        using (var appPathKey = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{executableName}"))
        {
            appPathKey.SetValue("", exePath);
            appPathKey.SetValue("Path", appDir);
        }

        var startMenuKeyPath = $@"SOFTWARE\Clients\StartMenuInternet\{appName}";
        using (var browserKey = Registry.LocalMachine.CreateSubKey(startMenuKeyPath))
        {
            browserKey.SetValue("", appName);
            
            using (var capabilities = browserKey.CreateSubKey("Capabilities"))
            {
                capabilities.SetValue("ApplicationName", appName);
                capabilities.SetValue("ApplicationDescription", AppDescription);
                capabilities.SetValue("AppUserModelId", $"{appName}.Browser");
                
                using (var urlAssociations = capabilities.CreateSubKey("URLAssociations"))
                {
                    urlAssociations.SetValue("http", $"{appName}.URL");
                    urlAssociations.SetValue("https", $"{appName}.URL");
                }
                
                using (var fileAssociations = capabilities.CreateSubKey("FileAssociations"))
                {
                    fileAssociations.SetValue(".htm", $"{appName}.URL");
                    fileAssociations.SetValue(".html", $"{appName}.URL");
                }
            }
            
            using (var defaultIcon = browserKey.CreateSubKey("DefaultIcon"))
            {
                defaultIcon.SetValue("", $"\"{exePath}\",0");
            }
            
            using (var commandKey = browserKey.CreateSubKey(@"shell\open\command"))
            {
                commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
            }
            
            using (var info = browserKey.CreateSubKey("InstallInfo"))
            {
                info.SetValue("IconsVisible", 1);
                info.SetValue("ShowIconsCommand", $"\"{exePath}\" --show-icons");
                info.SetValue("ReinstallCommand", $"\"{exePath}\" --reinstall");
                info.SetValue("HideIconsCommand", $"\"{exePath}\" --hide-icons");
            }
        }

        using (var regApps = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\RegisteredApplications"))
        {
            regApps.SetValue(appName, $@"SOFTWARE\Clients\StartMenuInternet\{appName}\Capabilities");
        }

        var progId = $"{appName}.URL";
        using (var progIdKey = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Classes\{progId}"))
        {
            progIdKey.SetValue("", $"{appName} URL");
            progIdKey.SetValue("FriendlyTypeName", $"{appName} URL");
            progIdKey.SetValue("AppUserModelId", $"{appName}.Browser");
            
            using (var defaultIcon = progIdKey.CreateSubKey("DefaultIcon"))
            {
                defaultIcon.SetValue("", $"\"{exePath}\",0");
            }
            
            using (var shell = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                shell.SetValue("", $"\"{exePath}\" \"%1\"");
            }
        }
        
        using (var uninstallKey = Registry.LocalMachine.CreateSubKey(
               $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{appName}"))
        {
            uninstallKey.SetValue("DisplayName", appName);
            uninstallKey.SetValue("DisplayIcon", $"\"{exePath}\",0");
            uninstallKey.SetValue("DisplayVersion", GetAppVersion());
            uninstallKey.SetValue("Publisher", "Vast the Doge & FoxyGuy716");
            uninstallKey.SetValue("URLInfoAbout", "https://github.com/VAST-THE-DOGE/FoxyBrowser716");
            uninstallKey.SetValue("InstallLocation", appDir);
            uninstallKey.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
            uninstallKey.SetValue("NoModify", 1);
            uninstallKey.SetValue("UninstallString", $"\"{exePath}\" --uninstall");
            uninstallKey.SetValue("QuietUninstallString", $"\"{exePath}\" --uninstall --quiet");
        }
                    
        CreateShortcuts(appName, exePath);
    }
    public static string GetExecutablePath()
    {
        return Process.GetCurrentProcess().MainModule?.FileName ?? 
               System.Reflection.Assembly.GetEntryAssembly()?.Location ?? 
               throw new InvalidOperationException("Could not determine executable path");
    }
    
    public static string GetApplicationName()
    {
        return Path.GetFileNameWithoutExtension(GetExecutablePath());
    }
    
    private static string GetAppVersion()
    {
        var assembly = System.Reflection.Assembly.GetEntryAssembly();
        var version = assembly?.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }
    
    private static bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    private static void RestartAsAdmin(string exePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--register",
            UseShellExecute = true,
            Verb = "runas"
        };
            
        try
        {
            Process.Start(startInfo);
        }
        catch(Win32Exception e)
        {
            MessageBox.Show("Install cannot be done properly without Administrative permissions. The app did not install.");
        }
    }
}