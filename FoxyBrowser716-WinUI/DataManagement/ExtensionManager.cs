using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Web;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using FoxyBrowser716_WinUI.DataObjects.Complex;
using Microsoft.Web.WebView2.Core;

namespace FoxyBrowser716_WinUI.DataManagement;

//TODO: break into different classes once everything works.
// Outline:
// L get current extensions from instance folder.
// L get manifest from extension folder as object IManifest and Manifest, ManifestV2, and ManifestV3.
// L extract extension id and store type from url.
// L build url from store and id.
// L get extension from url and unpack to a file.


// ---------- Shared helper types ----------

[JsonConverter(typeof(IconsConverter))]
public class Icons : Dictionary<string, string> { }

public class ActionInfo
{
    [JsonPropertyName("default_icon")]
    public Icons? DefaultIcon { get; set; }

    [JsonPropertyName("default_popup")]
    public string? DefaultPopup { get; set; }

    [JsonPropertyName("default_title")]
    public string? DefaultTitle { get; set; }
}

public class BackgroundV3
{
    [JsonPropertyName("service_worker")]
    public string? ServiceWorker { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("scripts")]
    public List<string>? Scripts { get; set; }

    [JsonPropertyName("persistent")]
    public bool? Persistent { get; set; }
}

public class BackgroundV2
{
    [JsonPropertyName("page")]
    public string? Page { get; set; }

    [JsonPropertyName("scripts")]
    public List<string>? Scripts { get; set; }

    [JsonPropertyName("persistent")]
    public bool? Persistent { get; set; }
}

public class ContentScript
{
    [JsonPropertyName("matches")]
    public List<string>? Matches { get; set; }

    [JsonPropertyName("exclude_matches")]
    public List<string>? ExcludeMatches { get; set; }

    [JsonPropertyName("js")]
    public List<string>? Js { get; set; }

    [JsonPropertyName("css")]
    public List<string>? Css { get; set; }

    [JsonPropertyName("run_at")]
    public string? RunAt { get; set; }

    [JsonPropertyName("all_frames")]
    public bool? AllFrames { get; set; }

    [JsonPropertyName("match_about_blank")]
    public bool? MatchAboutBlank { get; set; }

    [JsonPropertyName("world")]
    public string? World { get; set; }

    [JsonPropertyName("include_globs")]
    public List<string>? IncludeGlobs { get; set; }

    [JsonPropertyName("exclude_globs")]
    public List<string>? ExcludeGlobs { get; set; }
}

public class WebAccessibleResource
{
    [JsonPropertyName("resources")]
    public List<string>? Resources { get; set; }

    [JsonPropertyName("matches")]
    public List<string>? Matches { get; set; }

    [JsonPropertyName("extension_ids")]
    public List<string>? ExtensionIds { get; set; }

    [JsonPropertyName("use_dynamic_url")]
    public bool? UseDynamicUrl { get; set; }
}

public class OptionsUI
{
    [JsonPropertyName("open_in_tab")]
    public bool? OpenInTab { get; set; }

    [JsonPropertyName("page")]
    public string? Page { get; set; }

    [JsonPropertyName("chrome_style")]
    public bool? ChromeStyle { get; set; }
}

public class CommandDefinition
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("suggested_key")]
    public JsonElement? SuggestedKey { get; set; }

    [JsonPropertyName("global")]
    public bool? Global { get; set; }
}

// Custom converter for ContentSecurityPolicy which can be string or object
[JsonConverter(typeof(ContentSecurityPolicyConverter))]
public class ContentSecurityPolicy
{
    public string? ExtensionPages { get; set; }
    public string? SandboxedPages { get; set; }
    public string? IsolatedWorld { get; set; }
    
    // For backwards compatibility when it's just a string
    public string? Policy { get; set; }

    public override string? ToString() => Policy ?? ExtensionPages;
}

public class ExtensionManifestV3 : ExtensionManifestBase
{
    [JsonPropertyName("icons")]
    public Icons? Icons { get; set; }

    [JsonPropertyName("action")]
    public ActionInfo? Action { get; set; }

    [JsonPropertyName("background")]
    public BackgroundV3? Background { get; set; }

    [JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }

    [JsonPropertyName("host_permissions")]
    public List<string>? HostPermissions { get; set; }

    [JsonPropertyName("optional_permissions")]
    public List<string>? OptionalPermissions { get; set; }

    [JsonPropertyName("optional_host_permissions")]
    public List<string>? OptionalHostPermissions { get; set; }

    [JsonPropertyName("content_scripts")]
    public List<ContentScript>? ContentScripts { get; set; }

    [JsonPropertyName("web_accessible_resources")]
    [JsonConverter(typeof(WebAccessibleResourcesConverter))]
    public List<WebAccessibleResource>? WebAccessibleResources { get; set; }

    [JsonPropertyName("options_page")]
    public string? OptionsPage { get; set; }

    [JsonPropertyName("options_ui")]
    public OptionsUI? OptionsUI { get; set; }

    [JsonPropertyName("commands")]
    public Dictionary<string, CommandDefinition>? Commands { get; set; }

    [JsonPropertyName("incognito")]
    public string? Incognito { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("storage")]
    public JsonElement? Storage { get; set; }

    [JsonPropertyName("externally_connectable")]
    public JsonElement? ExternallyConnectable { get; set; }

    [JsonPropertyName("content_security_policy")]
    public ContentSecurityPolicy? ContentSecurityPolicy { get; set; }
}

public class ExtensionManifestV2 : ExtensionManifestBase
{
    [JsonPropertyName("icons")]
    public Icons? Icons { get; set; }

    [JsonPropertyName("browser_action")]
    public ActionInfo? BrowserAction { get; set; }

    [JsonPropertyName("page_action")]
    public ActionInfo? PageAction { get; set; }

    [JsonPropertyName("background")]
    public BackgroundV2? Background { get; set; }

    [JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }

    [JsonPropertyName("optional_permissions")]
    public List<string>? OptionalPermissions { get; set; }

    [JsonPropertyName("content_scripts")]
    public List<ContentScript>? ContentScripts { get; set; }

    [JsonPropertyName("options_ui")]
    public OptionsUI? OptionsUI { get; set; }

    [JsonPropertyName("web_accessible_resources")]
    public List<string>? WebAccessibleResources { get; set; }

    [JsonPropertyName("externally_connectable")]
    public JsonElement? ExternallyConnectable { get; set; }

    [JsonPropertyName("content_security_policy")]
    public string? ContentSecurityPolicy { get; set; }
}

// Converter for Icons (handles string or object)
public class IconsConverter : JsonConverter<Icons>
{
    public override Icons Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var icons = new Icons();

        if (reader.TokenType == JsonTokenType.String)
        {
            var iconPath = reader.GetString();
            if (!string.IsNullOrEmpty(iconPath))
            {
                icons["default"] = iconPath;
            }
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    icons[kvp.Key] = kvp.Value;
                }
            }
        }
        else if (reader.TokenType == JsonTokenType.Null)
        {
            // Handle null case
            return icons;
        }

        return icons;
    }

    public override void Write(Utf8JsonWriter writer, Icons value, JsonSerializerOptions options)
    {
        if (value.Count == 1 && value.ContainsKey("default"))
        {
            writer.WriteStringValue(value["default"]);
        }
        else
        {
            JsonSerializer.Serialize(writer, (Dictionary<string, string>)value, options);
        }
    }
}

// Converter for ContentSecurityPolicy (handles string or object)
public class ContentSecurityPolicyConverter : JsonConverter<ContentSecurityPolicy>
{
    public override ContentSecurityPolicy Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var csp = new ContentSecurityPolicy();

        if (reader.TokenType == JsonTokenType.String)
        {
            csp.Policy = reader.GetString();
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
            
            if (element.TryGetProperty("extension_pages", out var extPages))
                csp.ExtensionPages = extPages.GetString();
                
            if (element.TryGetProperty("sandboxed_pages", out var sandboxPages))
                csp.SandboxedPages = sandboxPages.GetString();
                
            if (element.TryGetProperty("isolated_world", out var isolatedWorld))
                csp.IsolatedWorld = isolatedWorld.GetString();
        }
        else if (reader.TokenType == JsonTokenType.Null)
        {
            return csp;
        }

        return csp;
    }

    public override void Write(Utf8JsonWriter writer, ContentSecurityPolicy value, JsonSerializerOptions options)
    {
        if (!string.IsNullOrEmpty(value.Policy))
        {
            writer.WriteStringValue(value.Policy);
        }
        else
        {
            writer.WriteStartObject();
            if (!string.IsNullOrEmpty(value.ExtensionPages))
                writer.WriteString("extension_pages", value.ExtensionPages);
            if (!string.IsNullOrEmpty(value.SandboxedPages))
                writer.WriteString("sandboxed_pages", value.SandboxedPages);
            if (!string.IsNullOrEmpty(value.IsolatedWorld))
                writer.WriteString("isolated_world", value.IsolatedWorld);
            writer.WriteEndObject();
        }
    }
}

// Enhanced WebAccessibleResourcesConverter
public class WebAccessibleResourcesConverter : JsonConverter<List<WebAccessibleResource>>
{
    public override List<WebAccessibleResource> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var outList = new List<WebAccessibleResource>();

        if (reader.TokenType == JsonTokenType.Null)
            return outList;

        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        if (element.ValueKind != JsonValueKind.Array) 
            return outList;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                outList.Add(new WebAccessibleResource { Resources = new List<string> { item.GetString()! } });
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                var war = new WebAccessibleResource();

                if (item.TryGetProperty("resources", out var resources))
                {
                    var list = new List<string>();
                    if (resources.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in resources.EnumerateArray())
                            if (r.ValueKind == JsonValueKind.String) 
                                list.Add(r.GetString()!);
                    }
                    else if (resources.ValueKind == JsonValueKind.String)
                    {
                        list.Add(resources.GetString()!);
                    }
                    war.Resources = list;
                }

                if (item.TryGetProperty("matches", out var matches))
                {
                    var list = new List<string>();
                    if (matches.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var m in matches.EnumerateArray())
                            if (m.ValueKind == JsonValueKind.String) 
                                list.Add(m.GetString()!);
                    }
                    else if (matches.ValueKind == JsonValueKind.String)
                    {
                        list.Add(matches.GetString()!);
                    }
                    war.Matches = list;
                }

                if (item.TryGetProperty("extension_ids", out var extIds))
                {
                    var list = new List<string>();
                    if (extIds.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var e in extIds.EnumerateArray())
                            if (e.ValueKind == JsonValueKind.String) 
                                list.Add(e.GetString()!);
                    }
                    else if (extIds.ValueKind == JsonValueKind.String)
                    {
                        list.Add(extIds.GetString()!);
                    }
                    war.ExtensionIds = list;
                }

                if (item.TryGetProperty("use_dynamic_url", out var useDynamicUrl))
                {
                    if (useDynamicUrl.ValueKind == JsonValueKind.True)
                        war.UseDynamicUrl = true;
                    else if (useDynamicUrl.ValueKind == JsonValueKind.False)
                        war.UseDynamicUrl = false;
                }

                outList.Add(war);
            }
        }

        return outList;
    }

    public override void Write(Utf8JsonWriter writer, List<WebAccessibleResource> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

public class LocalizedMessages
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Messages { get; set; }

    public string? GetMessage(string key)
    {
        if (Messages == null) return null;
        
        if (Messages.TryGetValue(key, out var element))
        {
            if (element.ValueKind == JsonValueKind.Object && 
                element.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString();
            }
            else if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
        }
        return null;
    }
}

public abstract class ExtensionManifestBase
{
    [JsonPropertyName("manifest_version")]
    public int ManifestVersion { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("short_name")]
    public string? ShortName { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("version_name")]
    public string? VersionName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("default_locale")]
    public string? DefaultLocale { get; set; }

    [JsonPropertyName("update_url")]
    public string? UpdateUrl { get; set; }

    [JsonPropertyName("minimum_chrome_version")]
    public string? MinimumChromeVersion { get; set; }

    [JsonPropertyName("homepage_url")]
    public string? HomepageUrl { get; set; }

    [JsonPropertyName("offline_enabled")]
    public bool? OfflineEnabled { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraData { get; set; }

    [JsonIgnore]
    public string? ExtensionFolderPath { get; set; }

    public T? GetExtraValue<T>(string key)
    {
        if (ExtraData == null) return default;
        if (!ExtraData.TryGetValue(key, out var el)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(el.GetRawText(), DefaultOptions);
        }
        catch
        {
            return default;
        }
    }

    // Get localized version of a field value
    public string? GetLocalizedValue(string? value, string language = "en")
    {
        if (string.IsNullOrEmpty(value) || !IsLocalizedString(value))
            return value;

        return ExtractLocalizedString(value, language) ?? value;
    }

    // Get localized name
    public string? GetLocalizedName(string language = "en")
    {
        return GetLocalizedValue(Name, language);
    }

    // Get localized short name
    public string? GetLocalizedShortName(string language = "en")
    {
        return GetLocalizedValue(ShortName, language);
    }

    // Get localized description
    public string? GetLocalizedDescription(string language = "en")
    {
        return GetLocalizedValue(Description, language);
    }

    // Check if a string is a localization key
    private static bool IsLocalizedString(string? value)
    {
        return !string.IsNullOrEmpty(value) && 
               value.StartsWith("__MSG_") && 
               value.EndsWith("__");
    }

    private string? ExtractLocalizedString(string localizedKey, string language = "en")
    {
        if (string.IsNullOrEmpty(ExtensionFolderPath) || !IsLocalizedString(localizedKey))
            return null;

        var messageKey = localizedKey.Substring(6, localizedKey.Length - 8);
        
        var localizedMessage = GetLocalizedMessage(messageKey, language);
        
        if (localizedMessage == null && !string.IsNullOrEmpty(DefaultLocale) && DefaultLocale != language)
        {
            localizedMessage = GetLocalizedMessage(messageKey, DefaultLocale);
        }
        
        if (localizedMessage == null && language != "en" && DefaultLocale != "en")
        {
            localizedMessage = GetLocalizedMessage(messageKey, "en");
        }

        return localizedMessage;
    }

    private string? GetLocalizedMessage(string messageKey, string language)
    {
        if (string.IsNullOrEmpty(ExtensionFolderPath))
            return null;

        var messagesPath = Path.Combine(ExtensionFolderPath, "_locales", language, "messages.json");
        
        if (!File.Exists(messagesPath))
            return null;

        try
        {
            var json = File.ReadAllText(messagesPath);
            var messages = JsonSerializer.Deserialize<LocalizedMessages>(json, DefaultOptions);
            return messages?.GetMessage(messageKey);
        }
        catch
        {
            return null;
        }
    }

    protected static JsonSerializerOptions DefaultOptions => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
}

public static class ExtensionManifestParser
{
    private static JsonSerializerOptions Options
    {
        get
        {
            var o = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
            o.Converters.Add(new WebAccessibleResourcesConverter());
            o.Converters.Add(new IconsConverter());
            o.Converters.Add(new ContentSecurityPolicyConverter());
            return o;
        }
    }

    public static ExtensionManifestBase Parse(string json, string? extensionFolderPath = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        int manifestVersion = 2;
        if (root.TryGetProperty("manifest_version", out var mv))
        {
            if (mv.ValueKind == JsonValueKind.Number && mv.TryGetInt32(out var iv)) 
                manifestVersion = iv;
            else if (mv.ValueKind == JsonValueKind.String && int.TryParse(mv.GetString(), out var sval)) 
                manifestVersion = sval;
        }

        ExtensionManifestBase result;
        try
        {
            if (manifestVersion >= 3)
            {
                var v3 = JsonSerializer.Deserialize<ExtensionManifestV3>(json, Options);
                result = v3 ?? throw new InvalidOperationException("Failed to deserialize as V3.");
            }
            else
            {
                var v2 = JsonSerializer.Deserialize<ExtensionManifestV2>(json, Options);
                result = v2 ?? throw new InvalidOperationException("Failed to deserialize as V2.");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse extension manifest: {ex.Message}", ex);
        }

        result.ExtensionFolderPath = extensionFolderPath;
        return result;
    }

    public static async Task<ExtensionManifestBase> ParseFromFileAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var extensionFolderPath = Path.GetDirectoryName(path);
        return Parse(json, extensionFolderPath);
    }
}








public static partial class ExtensionManager
{
	readonly static string[] _whitelist = ["Microsoft Clipboard Extension", "Microsoft Edge PDF Viewer"];
	
	/// <summary>
	/// Instance name to extension list
	/// </summary>
	private static ConcurrentDictionary<string, List<Extension>> _extensions = [];
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="webview"></param>
	/// <param name="instance"></param>
	public static async Task SetupExtensionSupport(this Instance instance, WebView2 webview)
	{
		// add extensions:
		if (_extensions.TryGetValue(instance.Name, out var extensions))
			await Task.WhenAll(extensions.Select(async e =>
				await webview.CoreWebView2.Profile.AddBrowserExtensionAsync(e.FolderPath)));
		else
		{
			var currentExtensions = await webview.CoreWebView2.Profile.GetBrowserExtensionsAsync();
			currentExtensions = currentExtensions.Where(e => !_whitelist.Contains(e.Name)).ToList();
			
			var extensionFolder = FoxyFileManager.BuildFolderPath(FoxyFileManager.FolderType.Extension, instance.Name);
			
			List<Extension> extensionsList = [];
			if (currentExtensions.Any())
			{
				await foreach (var ex in GetFolderExtensions(extensionFolder))
				{
					var webviewEx = currentExtensions.FirstOrDefault(e => IsNamesEqual(e.Name, ex.Manifest));
					
					if (webviewEx is null) continue;
					
					//TODO: THIS IS TEMP, make sure it is enabled for now and just keep a reference to this from the extension.
					await webviewEx.EnableAsync(true);
					
					extensionsList.Add(new Extension
					{
						FolderPath = ex.FolderPath,
						Manifest = ex.Manifest,
						WebviewName = webviewEx.Name,
						Id = webviewEx.Id
					});
				}
			}
			else
			{
				List<(Task<CoreWebView2BrowserExtension>, Extension)> tasks = [];
				await foreach (var ex in GetFolderExtensions(extensionFolder))
				{
					tasks.Add((
						webview.CoreWebView2.Profile
							.AddBrowserExtensionAsync(ex.FolderPath)
							.AsTask(),
						ex));
				}
				await Task.WhenAll(tasks.Select(p => p.Item1));
				tasks.ForEach(p => 
					extensionsList.Add(new Extension
					{
						FolderPath = p.Item2.FolderPath,
						Manifest = p.Item2.Manifest,
						WebviewName = p.Item1.Result.Name,
						Id = p.Item1.Result.Id
					}));
			}
			_extensions[instance.Name] = extensionsList;
		}
		
		// setup capturing of extension downloads:
		webview.CoreWebView2.DownloadStarting +=
			async (_, e) =>
			{
				// we only care about downloads from the chrome webstore for now,
				// skip anything else (aka, let the normal download handler take over):
				if (webview.CoreWebView2.Source.Contains("chromewebstore.google.com"))
				{
					e.Handled = true;
					await instance.AddExtension(webview, e);
				}
				
			};
	}

	private static async Task AddExtension(this Instance instance, WebView2 webview, CoreWebView2DownloadStartingEventArgs e)
	{
		if (ExtractExtensionIdFromUrl(e.DownloadOperation.Uri) is not { } id) return;
		await instance.AddExtension(webview, id);
	}

	public static async Task AddExtension(this Instance instance, WebView2 webview, string id)
{
    Debug.WriteLine($"Adding extension {id}");
    
    // Try multiple download methods in order of preference
    var methods = new[]
    {
        DownloadFromChromeWebStore,
        DownloadFromAlternativeEndpoint,
        DownloadFromCacheEndpoint
    };
    
    byte[] crxBytes = null;
    Exception lastException = null;
    
    foreach (var method in methods)
    {
        try
        {
            crxBytes = await method(id);
            if (crxBytes != null && crxBytes.Length > 250)
            {
                Debug.WriteLine($"Successfully downloaded extension {id} using method {method.Method.Name}");
                break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Method {method.Method.Name} failed for {id}: {ex.Message}");
            lastException = ex;
        }
    }
    
    if (crxBytes == null || crxBytes.Length <= 250)
    {
        throw new Exception($"Failed to download extension {id}. All methods failed. Last error: {lastException?.Message}");
    }
    
    await ProcessCrxFile(instance, webview, id, crxBytes);
}

private static async Task<byte[]> DownloadFromChromeWebStore(string id)
{
    using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
    
    // Use a more recent Chrome user agent
    http.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    
    // Add additional headers that Chrome typically sends
    http.DefaultRequestHeaders.Add("Accept", "*/*");
    http.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
    http.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
    http.DefaultRequestHeaders.Add("Pragma", "no-cache");
    http.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
    http.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
    http.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
    
    // Add a small delay to avoid rate limiting
    await Task.Delay(Random.Shared.Next(100, 500));
    
    var url = $"https://clients2.google.com/service/update2/crx?response=redirect&prodversion=120.0.0.0&acceptformat=crx2,crx3&x=id%3D{id}%26installsource%3Dondemand%26uc";
    
    using var resp = await http.GetAsync(url);
    resp.EnsureSuccessStatusCode();
    
    return await resp.Content.ReadAsByteArrayAsync();
}

private static async Task<byte[]> DownloadFromAlternativeEndpoint(string id)
{
    using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
    
    http.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    
    // Alternative endpoint that sometimes works better
    var url = $"https://chrome.google.com/webstore/detail/{id}";
    
    // First, get the extension page to potentially extract download links
    using var pageResp = await http.GetAsync(url);
    if (!pageResp.IsSuccessStatusCode)
    {
        throw new HttpRequestException($"Extension page not found: {pageResp.StatusCode}");
    }
    
    // Try the direct CRX download with different parameters
    var crxUrl = $"https://clients2.google.com/service/update2/crx?response=redirect&os=win&arch=x64&os_arch=x86_64&nacl_arch=x86-64&prod=chromecrx&prodchannel=stable&prodversion=120.0.0.0&lang=en-US&acceptformat=crx3&x=id%3D{id}%26installsource%3Dondemand%26uc";
    
    using var crxResp = await http.GetAsync(crxUrl);
    crxResp.EnsureSuccessStatusCode();
    
    return await crxResp.Content.ReadAsByteArrayAsync();
}

private static async Task<byte[]> DownloadFromCacheEndpoint(string id)
{
    using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
    
    http.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    
    // Try with cache headers to potentially get cached version
    http.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
    
    var url = $"https://update.googleapis.com/service/update2/crx?response=redirect&prodversion=120.0.0.0&x=id%3D{id}%26installsource%3Dondemand%26uc";
    
    using var resp = await http.GetAsync(url);
    resp.EnsureSuccessStatusCode();
    
    return await resp.Content.ReadAsByteArrayAsync();
}

private static async Task ProcessCrxFile(Instance instance, WebView2 webview, string id, byte[] crxBytes)
{
    var crxStream = new MemoryStream(crxBytes);
    crxStream.Position = 0;
    
    using var br = new BinaryReader(crxStream, Encoding.UTF8, leaveOpen: true);
    
    var signature = new string(br.ReadChars(4));
    if (signature != "Cr24")
    {
        throw new InvalidDataException("Invalid CRX header signature. Expected 'Cr24'.");
    }
    
    var version = br.ReadUInt32();
    
    switch (version)
    {
        case 2:
        {
            // CRX2 format:
            var publicKeySize = br.ReadUInt32();
            var signatureSize = br.ReadUInt32();
            
            br.ReadBytes((int)publicKeySize);  // Skip public key
            br.ReadBytes((int)signatureSize);  // Skip signature
            break;
        }
        case 3:
        {
            // CRX3 format:
            var headerSize = br.ReadUInt32();
            br.ReadBytes((int)headerSize);  // Skip entire header
            break;
        }
        default:
            throw new InvalidDataException($"Unsupported CRX version: {version}");
    }
    
    var zipDataSize = crxStream.Length - crxStream.Position;
    var zipData = new byte[zipDataSize];
    await crxStream.ReadExactlyAsync(zipData, 0, (int)zipDataSize);
    
    using var zipStream = new MemoryStream(zipData);
    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
    
    var outFolder = Path.Combine(FoxyFileManager.BuildFolderPath(FoxyFileManager.FolderType.Extension, instance.Name), id);
    
    if (!Directory.Exists(outFolder))
    {
        Directory.CreateDirectory(outFolder);
    }
    
    // Extract with error handling
    try
    {
        archive.ExtractToDirectory(outFolder, overwriteFiles: true);
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to extract extension {id}: {ex.Message}", ex);
    }
    
    var manifestPath = Path.Combine(outFolder, "manifest.json");
    if (!File.Exists(manifestPath))
    {
        throw new Exception($"No manifest.json found in extracted extension {id}! It will not be loaded.");
    }
    
    if (!_extensions.TryGetValue(instance.Name, out var extensions))
    {
        throw new Exception($"Extensions for instance name '{instance.Name}' not found");
    }
    
    var extension1 = await GetFolderExtension(outFolder);
    if (extension1 is null)
    {
        throw new Exception($"Failed to load extension from {outFolder}");
    }
    
    var currentExtensions = await webview.CoreWebView2.Profile.GetBrowserExtensionsAsync();
    currentExtensions = currentExtensions.Where(e => !_whitelist.Contains(e.Name)).ToList();
    
    // Remove the old one if it exists
    var webviewEx = currentExtensions.FirstOrDefault(e => IsNamesEqual(e.Name, extension1.Manifest));
    if (webviewEx != null)
    {
        await webviewEx.RemoveAsync();
    }
    
    var oldExtension = extensions.FirstOrDefault(e => e.Id == webviewEx?.Id);
    if (oldExtension is not null)
    {
        extensions.Remove(oldExtension);
    }
    
    // Add the new extension
    try
    {
        var browserExtension = await webview.CoreWebView2.Profile.AddBrowserExtensionAsync(outFolder);
        
        extensions.Add(new Extension
        {
            FolderPath = extension1.FolderPath,
            Manifest = extension1.Manifest,
            WebviewName = browserExtension.Name,
            Id = browserExtension.Id
        });
        
        Debug.WriteLine($"Successfully added extension {id} with WebView ID {browserExtension.Id}");
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to add extension {id} to WebView: {ex.Message}", ex);
    }
}

	private static string? ExtractExtensionIdFromUrl(string url)
	{
		try
		{
			if (string.IsNullOrEmpty(url)) return null;
			var uri = new Uri(url);

			var q = HttpUtility.ParseQueryString(uri.Query);
			var x = q["x"];
			if (!string.IsNullOrEmpty(x))
			{
				var decoded = HttpUtility.UrlDecode(x);
				var parts = decoded.Split(['&'], StringSplitOptions.RemoveEmptyEntries);
				foreach (var p in parts)
				{
					if (p.StartsWith("id=", StringComparison.OrdinalIgnoreCase))
						return p[3..];
				}
			}

			var idq = q["id"];
			if (!string.IsNullOrEmpty(idq)) return idq;

			var segs = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (segs.Length >= 3)
			{
				var candidate = segs.Last();
				if (System.Text.RegularExpressions.Regex.IsMatch(candidate, @"^[a-p0-9]{32}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
					return candidate;
			}

			var m = System.Text.RegularExpressions.Regex.Match(url, @"([a-p0-9]{32})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			if (m.Success) return m.Groups[1].Value;
		}
		catch { }

		return null;
	}
	
	private static bool IsNamesEqual(string name, ExtensionManifestBase manifest, string language = "en")
	{
		var localizedName = manifest.GetLocalizedName(language);
		var localizedShortName = manifest.GetLocalizedShortName(language);
    
		if (manifest is ExtensionManifestV2 v2)
		{
			var localizedTitle = manifest.GetLocalizedValue(v2.BrowserAction?.DefaultTitle, language);
			return name == localizedTitle || name == localizedName || name == localizedShortName;
		}
		else if (manifest is ExtensionManifestV3 v3)
		{
			var localizedTitle = manifest.GetLocalizedValue(v3.Action?.DefaultTitle, language);
			return name == localizedTitle || name == localizedName || name == localizedShortName;
		}
		else
			return false;
	}


	private static async IAsyncEnumerable<Extension> GetFolderExtensions(string extensionFolder)
	{
		var folders = await FoxyFileManager.GetChildrenOfFolderAsync(extensionFolder, FoxyFileManager.ItemType.Folder);

		foreach (var item in folders.items??[])
		{
			var manifestFile = Directory.GetFiles(item.path, "manifest.json", SearchOption.TopDirectoryOnly)
				.FirstOrDefault();

			if (manifestFile is null || await FoxyFileManager.ReadFromFileAsync(manifestFile) is not 
				    { code: FoxyFileManager.ReturnCode.Success, content: not null } result) continue;
			
			var manifest = ExtensionManifestParser.Parse(result.content, item.path);
			yield return new Extension { FolderPath = item.path, Manifest = manifest};
		}
	}

	public static async Task<Extension?> GetFolderExtension(string extensionFolder)
	{
		var manifestFile = Directory.GetFiles(extensionFolder, "manifest.json", SearchOption.TopDirectoryOnly)
			.FirstOrDefault();
		
		if (manifestFile is null || await FoxyFileManager.ReadFromFileAsync(manifestFile) is not 
			    { code: FoxyFileManager.ReturnCode.Success, content: not null } result) return null;
		
		var manifest = ExtensionManifestParser.Parse(result.content, extensionFolder);
		return new Extension { FolderPath = extensionFolder, Manifest = manifest};
	}

	public static List<Extension> GetSavedExtensions(this Instance instance)
	{
		return _extensions.TryGetValue(instance.Name, out var extensions) ? extensions : [];
	}
}

//TODO: buggy
/*
public static partial class ExtensionManager
{
    // map CoreWebView2 -> instance name so the global WebMessageReceived handler knows which instance
    static readonly ConcurrentDictionary<CoreWebView2, string> _webviewToInstance = new();
    // per-instance callback when the injected button is clicked
    static readonly ConcurrentDictionary<string, Action<JsonElement>> _storeButtonCallbacks = new();

    /// <summary>
    /// Register a callback to receive button clicks from the injected store button.
    /// Callback receives the JSON payload sent from the page as a JsonElement.
    /// </summary>
    public static void RegisterStoreButtonCallback(this Instance instance, Action<JsonElement> callback)
        => _storeButtonCallbacks[instance.Name] = callback;

    /// <summary>
    /// Unregister the callback for an instance.
    /// </summary>
    public static void UnregisterStoreButtonCallback(this Instance instance)
        => _storeButtonCallbacks.TryRemove(instance.Name, out _);

    /// <summary>
    /// Injects script that patches webstore/install buttons on commonly used store pages,
    /// sends click events back to host, and listens to host messages to update the button.
    /// Call this once after the WebView2 CoreWebView2 is created (or await initialization inside).
    /// </summary>
    public static async Task InjectStoreButtonInterceptor(this Instance instance, WebView2 webview)
    {
        if (webview is null) throw new ArgumentNullException(nameof(webview));
        if (instance is null) throw new ArgumentNullException(nameof(instance));

        // ensure core is initialized
        if (webview.CoreWebView2 == null)
            await webview.EnsureCoreWebView2Async();

        var core = webview.CoreWebView2;

        // Keep mapping to instance name (for the message handler)
        _webviewToInstance[core] = instance.Name;

        // Attach a WebMessageReceived handler (if not already attached). Use a single global handler.
        // (We attach it unconditionally — it's safe if you re-call; duplicates would cause double-calls,
        // so remove any previous subscription first if you want stricter control.)
        core.WebMessageReceived -= Core_WebMessageReceived;
        core.WebMessageReceived += Core_WebMessageReceived;

        // JS to inject (keeps a MutationObserver to work on SPA pages).
        // - Patches candidate buttons (searched by a few selectors + fallback text regex)
        // - Marks patched buttons with data-attribute so they can be updated later
        // - Posts messages to host on click
        // - Listens for host messages to update text/color/visibility
        var script = """
(function() {
  try {
    const STORE_HOSTS = [
      'chrome.google.com',
      'microsoftedge.microsoft.com',
      'microsoft.com',
      'addons.mozilla.org'
    ];

    function hostMatches() {
      return STORE_HOSTS.some(h => location.hostname.includes(h));
    }
    if (!hostMatches()) return;

    function isVisible(el) {
      try {
        const s = window.getComputedStyle(el);
        return s && s.display !== 'none' && s.visibility !== 'hidden' && el.offsetParent !== null;
      } catch (e) { return true; }
    }

    function findButtons() {
      const selectors = [
        'button[aria-label*="Add to Chrome"]',
        'button[aria-label*="Get"]',
        'button[aria-label*="Install"]',
        'button[aria-label*="Add to Edge"]',
        'button.btn-install',
        'a[role="button"]',
        'button'
      ];
      let list = [];
      selectors.forEach(sel => {
        try { list.push(...Array.from(document.querySelectorAll(sel))); } catch(e){}
      });

      // fallback: buttons or links whose text looks like an install/get button
      const textRegex = /(add to chrome|add to edge|install|get|add extension|add to browser|install extension)/i;
      list.push(...Array.from(document.querySelectorAll('button, a'))
        .filter(el => (el.innerText || el.textContent || '').trim() && textRegex.test((el.innerText || el.textContent || ''))));

      // dedupe
      const uniq = Array.from(new Set(list));
      return uniq.filter(isVisible);
    }

    function patchButtons() {
      const buttons = findButtons();
      for (const btn of buttons) {
        if (btn.dataset.__patched_store_btn === '1') continue;
        try {
          btn.dataset.__patched_store_btn = '1';
          btn.dataset.__orig_text = (btn.innerText || btn.textContent || '').trim();
          btn.style.transition = 'background-color .15s ease, color .15s ease';
          // default orange style
          btn.style.backgroundColor = '#ff8c00';
          btn.style.color = '#ffffff';
          // default text override - you can change from host
          try { btn.innerText = 'Install (custom)'; } catch(e){}
          btn.addEventListener('click', function(ev) {
            try {
              const payload = {
                type: 'storeButtonClick',
                host: location.hostname,
                url: location.href,
                originalText: btn.dataset.__orig_text || null
              };
              if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
                window.chrome.webview.postMessage(payload);
              }
            } catch (e){}
          }, { once: false });
        } catch (e) {}
      }
    }

    // observe DOM changes (SPA friendly)
    const mo = new MutationObserver((m) => {
      try { patchButtons(); } catch(e){}
    });
    mo.observe(document, { childList: true, subtree: true });

    // initial patch
    patchButtons();

    // listen for messages from host
    if (window.chrome && window.chrome.webview && window.chrome.webview.addEventListener) {
      window.chrome.webview.addEventListener('message', function(ev) {
        try {
          const msg = ev.data || {};
          if (msg.type === 'updateStoreButton') {
            const patched = document.querySelectorAll('[data-__patched_store_btn="1"]');
            patched.forEach(b => {
              try {
                if (msg.text !== undefined && msg.text !== null) {
                  b.innerText = msg.text;
                }
                if (msg.color) b.style.backgroundColor = msg.color;
                if (msg.color && !msg.text) b.style.color = '#fff';
                if (typeof msg.visible === 'boolean') b.style.display = msg.visible ? '' : 'none';
              } catch(e){}
            });
          }
        } catch(e){}
      });
    }
  } catch(e){}
})();
""";

        // ensure script runs on every navigation / new document
        await core.AddScriptToExecuteOnDocumentCreatedAsync(script);

        // also run immediately for the current document (if any content is already loaded)
        // ExecuteScriptAsync runs in the context of the page and can be used to apply immediate patch
        try { await core.ExecuteScriptAsync(script); } catch { /* ignore failures if execute not allowed #1# }
    }

    /// <summary>
    /// Send an update message to the page to change patched button(s):
    /// message: { type: 'updateStoreButton', text: '...', color: '#ff8c00', visible: true/false }
    /// </summary>
    public static void UpdateInjectedStoreButton(this Instance instance, WebView2 webview, string text = null, string color = null, bool? visible = null)
    {
        if (webview?.CoreWebView2 == null) return;
        var msgObj = new
        {
            type = "updateStoreButton",
            text,
            color,
            visible
        };
        var json = JsonSerializer.Serialize(msgObj, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        try
        {
            webview.CoreWebView2.PostWebMessageAsJson(json);
        } catch { /* swallow - host might not be ready #1# }
    }

    // Global WebMessageReceived handler used for all webviews we mapped earlier
    static void Core_WebMessageReceived(CoreWebView2 core, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            if (!_webviewToInstance.TryGetValue(core, out var instanceName)) return;

            // parse incoming JSON
            var json = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // dispatch on type
            if (root.TryGetProperty("type", out var tProp))
            {
                var type = tProp.GetString();
                if (type == "storeButtonClick")
                {
                    if (_storeButtonCallbacks.TryGetValue(instanceName, out var cb))
                    {
                        // pass the raw JsonElement so user code can inspect url/host/originalText
                        cb(root);
                    }
                    else
                    {
                        // default: just debug/log
                        try { System.Diagnostics.Debug.WriteLine($"Store button click from {instanceName}: {json}"); } catch { }
                    }
                }
            }
        }
        catch
        {
            // swallow any parse errors
        }
    }
}
*/
