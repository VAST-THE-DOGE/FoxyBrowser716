using System.IO;
using System.Reflection;

namespace FoxyBrowser716;

public static class InfoGetter
{
    #region FilePaths
    public static readonly string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"FoxyBrowser716");
    public static readonly string InstanceFolder = Path.Combine(AppData, "Instances");
    #endregion
    
    #region Versioning
    public static readonly Version? Version = Assembly.GetExecutingAssembly().GetName().Version;
    public static readonly string VersionString = Version is not null 
        ? $"v{Version.Major}.{Version.Minor}.{Version.Build}" 
        : "Unknown";
    #endregion
    
    public static readonly string AppName = Assembly.GetExecutingAssembly().GetName().FullName;

    #region URLs

    public const string GitHubURL = "https://github.com/VAST-THE-DOGE/FoxyBrowser716";

    #endregion
}