using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Web;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.DataObjects.Complex;
using Microsoft.Web.WebView2.Core;
using WinUIEx;

namespace FoxyBrowser716.DataManagement;

//TODO: break into different classes once everything works.
// Outline:
// L get current extensions from instance folder.
// L get manifest from extension folder as object IManifest and Manifest, ManifestV2, and ManifestV3.
// L extract extension id and store type from url.
// L build url from store and id.
// L get extension from url and unpack to a file.


#region  ManifestStuff
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

// Custom class for Author field which can be string or object
[JsonConverter(typeof(AuthorConverter))]
public class Author
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    
    public override string? ToString() => Name ?? Email;
    
    public static implicit operator string?(Author? author) => author?.ToString();
    public static implicit operator Author?(string? str) => str == null ? null : new Author { Name = str };
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
    public Author? Author { get; set; }

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

// Converter for Author (handles string or object)
public class AuthorConverter : JsonConverter<Author>
{
    public override Author Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var author = new Author();

        if (reader.TokenType == JsonTokenType.String)
        {
            author.Name = reader.GetString();
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
            
            if (element.TryGetProperty("name", out var name))
                author.Name = name.GetString();
                
            if (element.TryGetProperty("email", out var email))
                author.Email = email.GetString();
        }
        else if (reader.TokenType == JsonTokenType.Null)
        {
            return author;
        }

        return author;
    }

    public override void Write(Utf8JsonWriter writer, Author value, JsonSerializerOptions options)
    {
        if (!string.IsNullOrEmpty(value.Name) && string.IsNullOrEmpty(value.Email))
        {
            writer.WriteStringValue(value.Name);
        }
        else
        {
            writer.WriteStartObject();
            if (!string.IsNullOrEmpty(value.Name))
                writer.WriteString("name", value.Name);
            if (!string.IsNullOrEmpty(value.Email))
                writer.WriteString("email", value.Email);
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
            o.Converters.Add(new AuthorConverter());
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
#endregion

public static class ExtensionManager
{
    public static event Action<string>? ExtensionsModified;
    
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
        //setup JS for MS Store support:
        webview.CoreWebView2.WebMessageReceived += async (_,e) =>
        {
            string raw = e.TryGetWebMessageAsString();
            try {
                var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if(root.TryGetProperty("type", out var t)){
                    var type = t.GetString();
                    if(type == "installButtonClicked"){
                        var href = root.GetProperty("href").GetString();
                        var buttonId = root.GetProperty("buttonId").GetString();
                        Debug.WriteLine($"Install clicked: href={href} id={buttonId}");
                        var id = ExtractExtensionIdFromUrl(href);
                        if(id is null) return;
                        await instance.AddExtension(webview, id, ExtensionSource.Microsoft);
                    } else {
                        Debug.WriteLine("Page message: " + raw);
                    }
                }
            } catch(Exception ex){
                Debug.WriteLine("Failed parse web message: " + ex.Message + " raw:" + raw);
            }
        };
        
        await webview.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(MicrosoftStoreScript);

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

                // Ensure it's enabled
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
        ExtensionsModified?.Invoke(instance.Name);
		
		// setup capturing of extension downloads:
		webview.CoreWebView2.DownloadStarting +=
			async (_, e) =>
			{
				if (webview.CoreWebView2.Source.Contains("chromewebstore.google.com"))
				{
					e.Handled = true;
					await instance.AddExtension(webview, e);
				}
				
			};
	}

    public static async Task RemoveExtension(this Instance instance, WebView2 webview, string id)
    {
        if (_extensions.TryGetValue(instance.Name, out var extensions))
        {
            var webviewEx = (await webview.CoreWebView2.Profile.GetBrowserExtensionsAsync())
                .FirstOrDefault(e => e.Id == id);
            var localEx = extensions.FirstOrDefault(e => e.Id == id);
            
            if (webviewEx is null || localEx is null) return;

            webviewEx.RemoveAsync();
            extensions.Remove(localEx);
            FoxyFileManager.DeleteFolder(localEx.FolderPath);
            ExtensionsModified?.Invoke(instance.Name);
        }
    }
    
	private static async Task AddExtension(this Instance instance, WebView2 webview, CoreWebView2DownloadStartingEventArgs e)
	{
		if (ExtractExtensionIdFromUrl(e.DownloadOperation.Uri) is not { } id) return;
		await instance.AddExtension(webview, id, ExtensionSource.Chrome);
	}

    private enum ExtensionSource
    {
        Chrome,
        Microsoft,
    }
    
	private static async Task AddExtension(this Instance instance, WebView2 webview, string id, ExtensionSource source)
    {
        Debug.WriteLine($"Adding extension {id}");
        
        byte[] crxBytes = null;
        Exception lastException = null;

        if (source == ExtensionSource.Chrome)
        {
            //var url = $"https://clients2.google.com/service/update2/crx?response=redirect&prodversion=99&x=id%3D{id}%26uc";
            
            // from:
            // https://github.com/Rob--W/crxviewer/blob/master/src/cws_pattern.js
            
            const string product_channel = "unknown";
            const string product_version = "9999.0.9999.0";
            const string product_id = "chromiumcrx";
            const string platform = "win";
            var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "x86-32";
            
            var url = "https://clients2.google.com/service/update2/crx?response=redirect";
            url += $"&os={platform}";
            url += $"&arch={arch}";
            url += $"&os_arch={arch}";
            url += $"&nacl_arch={arch}";
            url += $"&prod={product_id}";
            url += $"&prodchannel={product_channel}";
            url += $"&prodversion={product_version}";
            url += $"&acceptformat=crx2,crx3";
            url += $"&x=id%3D{id}";
            url += $"%26uc";
            try
            {
                crxBytes = await DownloadFromUrl(url);
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }
        else if (source == ExtensionSource.Microsoft)
        {
            var url = $"https://edge.microsoft.com/extensionwebstorebase/v1/crx?response=redirect&x=id%3D{id}%26installsource%3Dondemand%26uc;";
            try
            {
                crxBytes = await DownloadFromUrl(url);
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }
        
        if (crxBytes == null || crxBytes.Length <= 250)
        {
            throw new Exception($"Failed to download extension {id}. All methods failed. Last error: {lastException?.Message}");
        }
        
        await ProcessCrxFile(instance, webview, id, crxBytes);
    }

    private static async Task<byte[]> DownloadFromUrl(string url, int i = 10)
    {
        if (i <= 0) throw new Exception("Failed to download extension. Maximum number of redirects reached.");
        
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        
        // Use a more recent Chrome user agent
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        
        // Add additional headers that Chrome typically sends
        http.DefaultRequestHeaders.Add("Accept", "*/*");
        http.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        http.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        http.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        http.DefaultRequestHeaders.Add("Pragma", "no-cache");
        http.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
        http.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
        http.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
        
        var response = await http.GetAsync(url);
    
        if (response.StatusCode is HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString();
            if (!string.IsNullOrEmpty(location))
            {
                return await DownloadFromUrl(location, --i);
            }
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
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
        
        ExtensionsModified?.Invoke(instance.Name);

        var messageBox = new ContentDialog()
        {
            Title = "Extension installed",
            PrimaryButtonText = "OK",
            XamlRoot = webview.XamlRoot,
        };
        await messageBox.ShowAsync();
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

    private const string MicrosoftStoreScript = //TODO a few lines under this in config of the JS:
        """
        // Inject this with AddScriptToExecuteOnDocumentCreatedAsync(...)
        (function(){
          if (window.__wv2_ext_helper_installed) return;
          window.__wv2_ext_helper_installed = true;
        
          // ===== CONFIG =====
          const DEFAULT_COLOR = 'rgb(0,116,204)'; // Microsoft blue
          const ALLOWED_HOST_PATTERNS = [/(\.|^)microsoftedge\.microsoft\.com$/i, /(\.|^)microsoft\.com$/i];
          // ====================
        
          let currentColor = DEFAULT_COLOR;
        
          function postButtonClick(msg){
            try {
              if (window.chrome?.webview?.postMessage) {
                chrome.webview.postMessage(JSON.stringify(msg));
              }
            } catch(e){
              console.warn('WebView2 postMessage failed:', e);
            }
          }
        
          function isAllowedHost(){
            try {
              const host = location.hostname || '';
              return ALLOWED_HOST_PATTERNS.some(re => re.test(host));
            } catch(e){ 
              return false; 
            }
          }
        
          function styleButton(btn){
            if (!btn) return;
            try{
              Object.assign(btn.style, {
                backgroundImage: 'none',
                backgroundColor: currentColor,
                borderColor: currentColor,
                color: '#ffffff',
                opacity: '1',
                pointerEvents: 'auto',
                cursor: 'pointer',
                borderRadius: btn.style.borderRadius || '4px'
              });
            } catch(e){
              console.warn('Button styling failed:', e);
            }
          }
        
          function removeIncompatibleNotices(){
            let removed = 0;
            try{
              // Remove elements with incompatible class
              document.querySelectorAll('.incompatible').forEach(el => {
                try{ 
                  el.remove(); 
                  removed++; 
                } catch(e){}
              });
        
              // Remove aria-live incompatible messages
              document.querySelectorAll('[aria-live]').forEach(el => {
                try {
                  if (el?.textContent?.includes('incompatible with your browser')){
                    el.remove(); 
                    removed++;
                  }
                } catch(e){}
              });
        
              // Remove other incompatible text
              const textElements = document.querySelectorAll('p, div, span');
              textElements.forEach(el => {
                try {
                  if (el?.textContent?.toLowerCase().includes('incompatible with your browser')){
                    el.remove(); 
                    removed++;
                  }
                } catch(e){}
              });
            } catch(e){
              console.warn('Remove incompatible notices failed:', e);
            }
            return removed;
          }
        
          function findInstallButtons(){
            const results = [];
            try {
              // Find buttons with install ID pattern
              const installButtons = document.querySelectorAll('button[id*="install"], button[id*="Install"]');
              installButtons.forEach(b => results.push(b));
        
              // Find "Get" buttons
              const buttons = document.querySelectorAll('button');
              buttons.forEach(b => {
                try {
                  const text = b?.textContent?.trim().toLowerCase();
                  if ((text === 'get' || text === 'install') && !results.includes(b)) {
                    results.push(b);
                  }
                } catch(e){}
              });
        
              // Find add-to-browser buttons
              const addButtons = document.querySelectorAll('button[aria-label*="Add"], button[title*="Add"]');
              addButtons.forEach(b => {
                if (!results.includes(b)) results.push(b);
              });
        
            } catch(e){
              console.warn('Find install buttons failed:', e);
            }
            return results;
          }
        
          function enableButtons(){
            const buttons = findInstallButtons();
            let changed = 0;
        
            buttons.forEach((btn, idx) => {
              if (!btn) return;
              
              try{
                // Enable the button
                btn.removeAttribute('disabled');
                btn.disabled = false;
                btn.removeAttribute('aria-disabled');
                
                // Remove disabled classes
                const disabledClasses = ['disabled', 'is-disabled', 'btn--disabled', 'fui-Button--disabled'];
                disabledClasses.forEach(cls => btn.classList.remove(cls));
                
                // Style the button
                styleButton(btn);
                
                // Add click handler once
                if (!btn.__wv2_click_hooked) {
                  btn.__wv2_click_hooked = true;
                  btn.addEventListener('click', function(ev){
                    postButtonClick({ 
                      type: 'installButtonClicked', 
                      href: location.href, 
                      buttonId: btn.id || `button-${idx}`,
                      buttonText: btn.textContent?.trim() || 'Unknown'
                    });
                  }, { capture: true, passive: true });
                }
                
                changed++;
              } catch(e){
                console.warn('Button enable failed:', e);
              }
            });
        
            return { count: buttons.length, changed };
          }
        
          function processPage(){
            if (!isAllowedHost()) {
              return { href: location.href, ignoredHost: true };
            }
        
            try {
              removeIncompatibleNotices();
              const buttonResult = enableButtons();
              
              return { 
                href: location.href, 
                buttonsFound: buttonResult.count, 
                buttonsChanged: buttonResult.changed
              };
            } catch(e) {
              console.error('Process page failed:', e);
              return { href: location.href, error: e.message };
            }
          }
        
          function startWatcher(){
            if (!isAllowedHost()) return;
            
            let attempts = 0;
            const maxAttempts = 30;
            const observer = new MutationObserver(() => {
              attempts++;
              try {
                const result = processPage();
                if (result.buttonsFound > 0 || attempts >= maxAttempts) {
                  observer.disconnect();
                }
              } catch(e) {
                console.warn('Watcher iteration failed:', e);
              }
            });
        
            try {
              const target = document.documentElement || document.body || document;
              observer.observe(target, { 
                childList: true, 
                subtree: true, 
                attributes: true 
              });
              
              // Initial run
              processPage();
              
              // Timeout fallback
              setTimeout(() => {
                try {
                  observer.disconnect();
                } catch(e){}
              }, 10000);
              
            } catch(e) {
              console.error('Watcher setup failed:', e);
              processPage(); // Fallback to single run
            }
          }
        
          // Navigation handling for SPAs
          function setupNavigationHandler() {
            const originalPushState = history.pushState;
            const originalReplaceState = history.replaceState;
            
            history.pushState = function() {
              const result = originalPushState.apply(this, arguments);
              setTimeout(() => startWatcher(), 100);
              return result;
            };
            
            history.replaceState = function() {
              const result = originalReplaceState.apply(this, arguments);
              setTimeout(() => startWatcher(), 100);
              return result;
            };
            
            window.addEventListener('popstate', () => {
              setTimeout(() => startWatcher(), 100);
            });
          }
        
          // Message handler for host commands
          function setupMessageHandler() {
            try {
              if (window.chrome?.webview?.addEventListener) {
                chrome.webview.addEventListener('message', function(e){
                  try {
                    const data = typeof e.data === 'string' ? JSON.parse(e.data) : e.data;
                    if (!data) return;
        
                    switch(data.cmd) {
                      case 'setColor':
                        if (typeof data.color === 'string') {
                          currentColor = data.color.startsWith('rgb') ? data.color : `rgb(${data.color})`;
                          findInstallButtons().forEach(styleButton);
                        }
                        break;
                        
                      case 'runNow':
                        processPage();
                        break;
                    }
                  } catch(e) {
                    console.warn('Message handler failed:', e);
                  }
                });
              }
            } catch(e) {
              console.warn('Message handler setup failed:', e);
            }
          }
        
          // Initialize
          function init() {
            setupNavigationHandler();
            setupMessageHandler();
            
            // Export helper functions
            window.__wv2_ext_helper = { 
              runNow: processPage, 
              setColor: (color) => {
                currentColor = color;
                findInstallButtons().forEach(styleButton);
              }
            };
        
            // Start processing
            if (document.readyState === 'complete' || document.readyState === 'interactive') {
              startWatcher();
            } else {
              window.addEventListener('DOMContentLoaded', startWatcher, { once: true });
              window.addEventListener('load', startWatcher, { once: true });
            }
          }
        
          init();
        })();
        """;
}