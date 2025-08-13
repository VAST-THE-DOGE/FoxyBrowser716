using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using FoxyBrowser716_WinUI.DataObjects.Complex;

namespace FoxyBrowser716_WinUI.DataManagement;

//TODO: break into different classes once everything works.
// Outline:
// L get current extensions from instance folder.
// L get manifest from extension folder as object IManifest and Manifest, ManifestV2, and ManifestV3.
// L extract extension id and store type from url.
// L build url from store and id.
// L get extension from url and unpack to a file.


// ---------- Shared helper types ----------
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

    // sometimes there's a "type": "module"
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class BackgroundV2
{
    [JsonPropertyName("page")]
    public string? Page { get; set; }
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

    // sometimes extensions specify a "world" (e.g., "MAIN")
    [JsonPropertyName("world")]
    public string? World { get; set; }
}

public class WebAccessibleResource
{
    // "resources" is the modern v3 object form
    [JsonPropertyName("resources")]
    public List<string>? Resources { get; set; }

    // fallback: some manifests (v2) use an array of strings; converter handles that
    [JsonPropertyName("matches")]
    public List<string>? Matches { get; set; }

    [JsonPropertyName("extension_ids")]
    public List<string>? ExtensionIds { get; set; }
}

public class OptionsUI
{
    [JsonPropertyName("open_in_tab")]
    public bool? OpenInTab { get; set; }

    [JsonPropertyName("page")]
    public string? Page { get; set; }
}

public class CommandDefinition
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("suggested_key")]
    public JsonElement? SuggestedKey { get; set; } // can be object or string
}

// ---------- Manifest V3 ----------
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

    [JsonPropertyName("content_scripts")]
    public List<ContentScript>? ContentScripts { get; set; }

    // web_accessible_resources can be either array-of-strings (v2 style) or array-of-objects (v3 style)
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
    public JsonElement? Storage { get; set; } // often an object with managed_schema, keep as JsonElement
}

// ---------- Manifest V2 ----------
public class ExtensionManifestV2 : ExtensionManifestBase
{
    [JsonPropertyName("icons")]
    public Icons? Icons { get; set; }

    [JsonPropertyName("browser_action")]
    public ActionInfo? BrowserAction { get; set; }

    [JsonPropertyName("background")]
    public BackgroundV2? Background { get; set; }

    [JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }

    [JsonPropertyName("content_scripts")]
    public List<ContentScript>? ContentScripts { get; set; }

    [JsonPropertyName("options_ui")]
    public OptionsUI? OptionsUI { get; set; }

    [JsonPropertyName("web_accessible_resources")]
    public List<string>? WebAccessibleResources { get; set; } // v2 common shape
}

// ---------- Converter: web_accessible_resources (handles both strings and objects) ----------
public class WebAccessibleResourcesConverter : JsonConverter<List<WebAccessibleResource>>
{
    public override List<WebAccessibleResource> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var outList = new List<WebAccessibleResource>();

        // Deserialize to JsonElement for easy inspection
        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        if (element.ValueKind != JsonValueKind.Array) return outList;

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
                    foreach (var r in resources.EnumerateArray())
                        if (r.ValueKind == JsonValueKind.String) list.Add(r.GetString()!);
                    war.Resources = list;
                }

                if (item.TryGetProperty("matches", out var matches))
                {
                    var list = new List<string>();
                    foreach (var m in matches.EnumerateArray())
                        if (m.ValueKind == JsonValueKind.String) list.Add(m.GetString()!);
                    war.Matches = list;
                }

                if (item.TryGetProperty("extension_ids", out var extIds))
                {
                    var list = new List<string>();
                    foreach (var e in extIds.EnumerateArray())
                        if (e.ValueKind == JsonValueKind.String) list.Add(e.GetString()!);
                    war.ExtensionIds = list;
                }

                outList.Add(war);
            }
            // ignore other kinds
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

// Add localization helper methods to ExtensionManifestBase
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

    [JsonPropertyName("content_security_policy")]
    public string? ContentSecurityPolicy { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraData { get; set; }

    // Store the extension folder path for localization
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

    // Extract the actual localized string
    private string? ExtractLocalizedString(string localizedKey, string language = "en")
    {
        if (string.IsNullOrEmpty(ExtensionFolderPath) || !IsLocalizedString(localizedKey))
            return null;

        // Extract the message key (remove __MSG_ and __)
        var messageKey = localizedKey.Substring(6, localizedKey.Length - 8);
        
        // Try to get the localized message
        var localizedMessage = GetLocalizedMessage(messageKey, language);
        
        // Fallback to default locale if specified and different from requested language
        if (localizedMessage == null && !string.IsNullOrEmpty(DefaultLocale) && DefaultLocale != language)
        {
            localizedMessage = GetLocalizedMessage(messageKey, DefaultLocale);
        }
        
        // Final fallback to "en" if not already tried
        if (localizedMessage == null && language != "en" && DefaultLocale != "en")
        {
            localizedMessage = GetLocalizedMessage(messageKey, "en");
        }

        return localizedMessage;
    }

    // Get a specific message from the locales files
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

// Update the parser to set the extension folder path
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
            return o;
        }
    }

    public static ExtensionManifestBase Parse(string json, string? extensionFolderPath = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        int manifestVersion = 2; // fallback
        if (root.TryGetProperty("manifest_version", out var mv))
        {
            if (mv.ValueKind == JsonValueKind.Number && mv.TryGetInt32(out var iv)) manifestVersion = iv;
            else if (mv.ValueKind == JsonValueKind.String && int.TryParse(mv.GetString(), out var sval)) manifestVersion = sval;
        }

        ExtensionManifestBase result;
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

        // Set the extension folder path for localization
        result.ExtensionFolderPath = extensionFolderPath;
        return result;
    }

    public static async Task<ExtensionManifestBase> ParseFromFileAsync(string path)
    {
        //TODO: move to using FoxyFileManager
        
        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var extensionFolderPath = Path.GetDirectoryName(path);
        return Parse(json, extensionFolderPath);
    }
}








public static class ExtensionManager
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
	public static async Task AddExtensions(this Instance instance, WebView2 webview)
	{
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
				await foreach (var ex in GetExtension(extensionFolder))
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
				List<Task> tasks = [];
				await foreach (var ex in GetExtension(extensionFolder))
				{
					tasks.Add(
						webview.CoreWebView2.Profile
							.AddBrowserExtensionAsync(ex.FolderPath)
							.AsTask()
							.ContinueWith(T => 
								extensionsList.Add(new Extension
									{
										FolderPath = ex.FolderPath,
										Manifest = ex.Manifest,
										WebviewName = T.Result.Name,
										Id = T.Result.Id
									}
								)
							)
					);
				}
				await Task.WhenAll(tasks);
			}
			_extensions[instance.Name] = extensionsList;
		}
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


	private static async IAsyncEnumerable<Extension> GetExtension(string extensionFolder)
	{
		var folders = await FoxyFileManager.GetChildrenOfFolderAsync(extensionFolder, FoxyFileManager.ItemType.Folder);

		foreach (var item in folders.items??[])
		{
			var manifestFile = Directory.GetFiles(item.path, "manifest.json", SearchOption.TopDirectoryOnly)
				.FirstOrDefault();

			if (manifestFile is null || FoxyFileManager.ReadFromFile(manifestFile) is not 
				    { code: FoxyFileManager.ReturnCode.Success, content: not null } result) continue;
			
			var manifest = ExtensionManifestParser.Parse(result.content, item.path);
			yield return new Extension { FolderPath = item.path, Manifest = manifest};
		}
	}

	public static List<Extension> GetExtensions(this Instance instance)
	{
		return _extensions.TryGetValue(instance.Name, out var extensions) ? extensions : [];
	}
}
