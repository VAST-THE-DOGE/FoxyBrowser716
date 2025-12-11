using System.Reflection;

namespace FoxyBrowser716.StaticData;

public static class InfoGetter
{
    #region FilePaths
    //TODO: maybe keep in here, or in the middleware?
    #endregion

    #region Versioning
    public static readonly Version? Version = Assembly.GetExecutingAssembly().GetName().Version;
    public static readonly string VersionString = Version is not null
        ? $"v{Version.Major}.{Version.Minor}.{Version.Build}.{Version.Revision}"
        : "Unknown";

    public static readonly string LeadDev = "Vast The Doge (William Herbert)";
    public static readonly string[] OtherDevs = [];
    public static readonly string[] Contributors = ["FoxyGuy716"];
    #endregion

    public static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name;

    #region URLs
    public const string GitHubUrl = "https://github.com/VAST-THE-DOGE/FoxyBrowser716";
    public const string WebsiteUrl = "https://FoxyBrowser716.com";
    public const string LatestVersionApiUrl = "https://foxybrowser716.com/api/latest-version";
    
    public static string GetSearchCompletionUrl(string query) => $"https://suggestqueries.google.com/complete/search?client=chrome&q={Uri.EscapeDataString(query)}";
    #endregion

    #region SearchEngines
    public enum SearchEngine
    {
        Bing,
        Google,
        DuckDuckGo,
        Yahoo,
        Wikipedia,
        Amazon,
        Newegg,
        YouTube,
        StackOverflow,
    }

    public static string GetSearchEngineName(SearchEngine engine) => engine switch
    {
        SearchEngine.Google => "Google",
        SearchEngine.DuckDuckGo => "DuckDuckGo",
        SearchEngine.Bing => "Bing",
        SearchEngine.Yahoo => "Yahoo",
        SearchEngine.Amazon => "Amazon",
        SearchEngine.Newegg => "Newegg",
        SearchEngine.Wikipedia => "Wikipedia",
        SearchEngine.YouTube => "YouTube",
        SearchEngine.StackOverflow => "Stack Overflow",
        _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, null)
    };

    public static string GetSearchEngineIcon(SearchEngine engine) => engine switch
    {
        SearchEngine.Google => "https://www.google.com/favicon.ico",
        SearchEngine.DuckDuckGo => "https://duckduckgo.com/favicon.ico",
        SearchEngine.Bing => "https://www.bing.com/favicon.ico",
        SearchEngine.Yahoo => "https://www.yahoo.com/favicon.ico",
        SearchEngine.Amazon => "https://www.amazon.com/favicon.ico",
        SearchEngine.Newegg => "https://www.newegg.com/favicon.ico",
        SearchEngine.Wikipedia => "https://en.wikipedia.org/favicon.ico", 
        SearchEngine.YouTube => "https://www.youtube.com/favicon.ico",
        SearchEngine.StackOverflow => "https://stackoverflow.com/favicon.ico",
        _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, null)
    };

    public static string GetSearchUrl(SearchEngine engine, string query) => engine switch
    {
        SearchEngine.Google => $"https://www.google.com/search?q={Uri.EscapeDataString(query)}",
        SearchEngine.DuckDuckGo => $"https://duckduckgo.com/?q={Uri.EscapeDataString(query)}",
        SearchEngine.Bing => $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}",
        SearchEngine.Yahoo => $"https://search.yahoo.com/search?p={Uri.EscapeDataString(query)}",
        SearchEngine.Amazon => $"https://www.amazon.com/s?k={Uri.EscapeDataString(query)}",
        SearchEngine.Newegg => $"https://www.newegg.com/p/pl?d={Uri.EscapeDataString(query)}",
        SearchEngine.Wikipedia => $"https://en.wikipedia.org/wiki/Special:Search?search={Uri.EscapeDataString(query)}",
        SearchEngine.YouTube => $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}",
        SearchEngine.StackOverflow => $"https://stackoverflow.com/search?q={Uri.EscapeDataString(query)}",
        _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, null)
    };

    #endregion
}
