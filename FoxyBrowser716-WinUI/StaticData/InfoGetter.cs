using System.Reflection;

namespace FoxyBrowser716_WinUI.StaticData;

public static class InfoGetter
{
    #region FilePaths
    //TODO: maybe keep in here, or in the middleware?
    #endregion
    
    #region Versioning
    public static readonly Version? Version = Assembly.GetExecutingAssembly().GetName().Version;
    public static readonly string VersionString = Version is not null 
        ? $"v{Version.Major}.{Version.Minor}.{Version.Build}" 
        : "Unknown";

    public static readonly string LeadDev = "Vast The Doge (William Herbert)";
    public static readonly string[] OtherDevs = [];
    public static readonly string[] Contributors = ["FoxyGuy716"];
    #endregion
    
    public static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name;

    #region URLs

    public const string GitHubURL = "https://github.com/VAST-THE-DOGE/FoxyBrowser716";

    #endregion

    #region SearchEngines
    public enum SearchEngine
    {
        Google,
        DuckDuckGo,
        Bing,
        Yahoo,
    }
    
    public static string GetSearchEngineName(SearchEngine engine) => engine switch
    {
        SearchEngine.Google => "Google",
        SearchEngine.DuckDuckGo => "DuckDuckGo",
        SearchEngine.Bing => "Bing",
        SearchEngine.Yahoo => "Yahoo",
        _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, null)
    };
    
    public static string GetSearchEngineIcon(SearchEngine engine) => engine switch
    {
        SearchEngine.Google => "https://www.google.com/favicon.ico", //TODO might work
        SearchEngine.DuckDuckGo => "https://duckduckgo.com/favicon.ico",
        SearchEngine.Bing => "https://www.bing.com/favicon.ico",
        SearchEngine.Yahoo => "https://www.yahoo.com/favicon.ico",
        _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, null)
    };
    
    public static string GetSearchUrl(SearchEngine engine, string query) => engine switch
    {
        SearchEngine.Google => $"https://www.google.com/search?q={Uri.EscapeDataString(query)}",
        SearchEngine.DuckDuckGo => $"https://duckduckgo.com/?q={Uri.EscapeDataString(query)}",
        SearchEngine.Bing => $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}",
        SearchEngine.Yahoo => $"https://search.yahoo.com/search?p={Uri.EscapeDataString(query)}",
        _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, null)
    };

    #endregion
}