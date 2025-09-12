using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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

    [JsonPropertyName("world")]
    public string? World { get; set; }
}

public class WebAccessibleResource
{
    [JsonPropertyName("resources")]
    public List<string>? Resources { get; set; }

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
    public JsonElement? SuggestedKey { get; set; }
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
}

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
    public List<string>? WebAccessibleResources { get; set; }
}

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

    [JsonPropertyName("content_security_policy")]
    public string? ContentSecurityPolicy { get; set; }

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
