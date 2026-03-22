// using System.Text.Json.Serialization;
// using System.Text.Json.Serialization.Metadata;
//
// namespace FoxyBrowser716.DataObjects.Complex;
//
// // TODO simplify later based on needs
//
//
// [JsonConverter(typeof(IconsConverter))]
// public class Icons : Dictionary<string, string> { }
//
// public class ActionInfo
// {
//     [JsonPropertyName("default_icon")]
//     public Icons? DefaultIcon { get; set; }
//
//     [JsonPropertyName("default_popup")]
//     public string? DefaultPopup { get; set; }
//
//     [JsonPropertyName("default_title")]
//     public string? DefaultTitle { get; set; }
// }
//
// public class BackgroundV3
// {
//     [JsonPropertyName("service_worker")]
//     public string? ServiceWorker { get; set; }
//
//     [JsonPropertyName("type")]
//     public string? Type { get; set; }
//
//     [JsonPropertyName("scripts")]
//     public List<string>? Scripts { get; set; }
//
//     [JsonPropertyName("persistent")]
//     public bool? Persistent { get; set; }
// }
//
// public class BackgroundV2
// {
//     [JsonPropertyName("page")]
//     public string? Page { get; set; }
//
//     [JsonPropertyName("scripts")]
//     public List<string>? Scripts { get; set; }
//
//     [JsonPropertyName("persistent")]
//     public bool? Persistent { get; set; }
// }
//
// public class ContentScript
// {
//     [JsonPropertyName("matches")]
//     public List<string>? Matches { get; set; }
//
//     [JsonPropertyName("exclude_matches")]
//     public List<string>? ExcludeMatches { get; set; }
//
//     [JsonPropertyName("js")]
//     public List<string>? Js { get; set; }
//
//     [JsonPropertyName("css")]
//     public List<string>? Css { get; set; }
//
//     [JsonPropertyName("run_at")]
//     public string? RunAt { get; set; }
//
//     [JsonPropertyName("all_frames")]
//     public bool? AllFrames { get; set; }
//
//     [JsonPropertyName("match_about_blank")]
//     public bool? MatchAboutBlank { get; set; }
//
//     [JsonPropertyName("world")]
//     public string? World { get; set; }
//
//     [JsonPropertyName("include_globs")]
//     public List<string>? IncludeGlobs { get; set; }
//
//     [JsonPropertyName("exclude_globs")]
//     public List<string>? ExcludeGlobs { get; set; }
// }
//
// public class WebAccessibleResource
// {
//     [JsonPropertyName("resources")]
//     public List<string>? Resources { get; set; }
//
//     [JsonPropertyName("matches")]
//     public List<string>? Matches { get; set; }
//
//     [JsonPropertyName("extension_ids")]
//     public List<string>? ExtensionIds { get; set; }
//
//     [JsonPropertyName("use_dynamic_url")]
//     public bool? UseDynamicUrl { get; set; }
// }
//
// public class OptionsUI
// {
//     [JsonPropertyName("open_in_tab")]
//     public bool? OpenInTab { get; set; }
//
//     [JsonPropertyName("page")]
//     public string? Page { get; set; }
//
//     [JsonPropertyName("chrome_style")]
//     public bool? ChromeStyle { get; set; }
// }
//
// public class CommandDefinition
// {
//     [JsonPropertyName("description")]
//     public string? Description { get; set; }
//
//     [JsonPropertyName("suggested_key")]
//     public JsonElement? SuggestedKey { get; set; }
//
//     [JsonPropertyName("global")]
//     public bool? Global { get; set; }
// }
//
// // Custom converter for ContentSecurityPolicy which can be string or object
// [JsonConverter(typeof(ContentSecurityPolicyConverter))]
// public class ContentSecurityPolicy
// {
//     public string? ExtensionPages { get; set; }
//     public string? SandboxedPages { get; set; }
//     public string? IsolatedWorld { get; set; }
//     
//     // For backwards compatibility when it's just a string
//     public string? Policy { get; set; }
//
//     public override string? ToString() => Policy ?? ExtensionPages;
// }
//
// // Custom class for Author field which can be string or object
// [JsonConverter(typeof(AuthorConverter))]
// public class Author
// {
//     public string? Name { get; set; }
//     public string? Email { get; set; }
//     
//     public override string? ToString() => Name ?? Email;
//     
//     public static implicit operator string?(Author? author) => author?.ToString();
//     public static implicit operator Author?(string? str) => str == null ? null : new Author { Name = str };
// }
//
// public class ExtensionManifestV3 : ExtensionManifestBase
// {
//     [JsonPropertyName("icons")]
//     public Icons? Icons { get; set; }
//
//     [JsonPropertyName("action")]
//     public ActionInfo? Action { get; set; }
//
//     [JsonPropertyName("background")]
//     public BackgroundV3? Background { get; set; }
//
//     [JsonPropertyName("permissions")]
//     public List<string>? Permissions { get; set; }
//
//     [JsonPropertyName("host_permissions")]
//     public List<string>? HostPermissions { get; set; }
//
//     [JsonPropertyName("optional_permissions")]
//     public List<string>? OptionalPermissions { get; set; }
//
//     [JsonPropertyName("optional_host_permissions")]
//     public List<string>? OptionalHostPermissions { get; set; }
//
//     [JsonPropertyName("content_scripts")]
//     public List<ContentScript>? ContentScripts { get; set; }
//
//     [JsonPropertyName("web_accessible_resources")]
//     [JsonConverter(typeof(WebAccessibleResourcesConverter))]
//     public List<WebAccessibleResource>? WebAccessibleResources { get; set; }
//
//     [JsonPropertyName("options_page")]
//     public string? OptionsPage { get; set; }
//
//     [JsonPropertyName("options_ui")]
//     public OptionsUI? OptionsUI { get; set; }
//
//     [JsonPropertyName("commands")]
//     public Dictionary<string, CommandDefinition>? Commands { get; set; }
//
//     [JsonPropertyName("incognito")]
//     public string? Incognito { get; set; }
//
//     [JsonPropertyName("author")]
//     public Author? Author { get; set; }
//
//     [JsonPropertyName("storage")]
//     public JsonElement? Storage { get; set; }
//
//     [JsonPropertyName("externally_connectable")]
//     public JsonElement? ExternallyConnectable { get; set; }
//
//     [JsonPropertyName("content_security_policy")]
//     public ContentSecurityPolicy? ContentSecurityPolicy { get; set; }
// }
//
// public class ExtensionManifestV2 : ExtensionManifestBase
// {
//     [JsonPropertyName("icons")]
//     public Icons? Icons { get; set; }
//
//     [JsonPropertyName("browser_action")]
//     public ActionInfo? BrowserAction { get; set; }
//
//     [JsonPropertyName("page_action")]
//     public ActionInfo? PageAction { get; set; }
//
//     [JsonPropertyName("background")]
//     public BackgroundV2? Background { get; set; }
//
//     [JsonPropertyName("permissions")]
//     public List<string>? Permissions { get; set; }
//
//     [JsonPropertyName("optional_permissions")]
//     public List<string>? OptionalPermissions { get; set; }
//
//     [JsonPropertyName("content_scripts")]
//     public List<ContentScript>? ContentScripts { get; set; }
//
//     [JsonPropertyName("options_ui")]
//     public OptionsUI? OptionsUI { get; set; }
//
//     [JsonPropertyName("web_accessible_resources")]
//     public List<string>? WebAccessibleResources { get; set; }
//
//     [JsonPropertyName("externally_connectable")]
//     public JsonElement? ExternallyConnectable { get; set; }
//
//     [JsonPropertyName("content_security_policy")]
//     public string? ContentSecurityPolicy { get; set; }
// }
//
// // Converter for Icons (handles string or object)
// public class IconsConverter : JsonConverter<Icons>
// {
//     public override Icons Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//     {
//         var icons = new Icons();
//
//         if (reader.TokenType == JsonTokenType.String)
//         {
//             var iconPath = reader.GetString();
//             if (!string.IsNullOrEmpty(iconPath))
//             {
//                 icons["default"] = iconPath;
//             }
//         }
//         else if (reader.TokenType == JsonTokenType.StartObject)
//         {
//             var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
//             if (dict != null)
//             {
//                 foreach (var kvp in dict)
//                 {
//                     icons[kvp.Key] = kvp.Value;
//                 }
//             }
//         }
//         else if (reader.TokenType == JsonTokenType.Null)
//         {
//             // Handle null case
//             return icons;
//         }
//
//         return icons;
//     }
//
//     public override void Write(Utf8JsonWriter writer, Icons value, JsonSerializerOptions options)
//     {
//         if (value.Count == 1 && value.ContainsKey("default"))
//         {
//             writer.WriteStringValue(value["default"]);
//         }
//         else
//         {
//             JsonSerializer.Serialize(writer, (Dictionary<string, string>)value, options);
//         }
//     }
// }
//
// // Converter for ContentSecurityPolicy (handles string or object)
// public class ContentSecurityPolicyConverter : JsonConverter<ContentSecurityPolicy>
// {
//     public override ContentSecurityPolicy Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//     {
//         var csp = new ContentSecurityPolicy();
//
//         if (reader.TokenType == JsonTokenType.String)
//         {
//             csp.Policy = reader.GetString();
//         }
//         else if (reader.TokenType == JsonTokenType.StartObject)
//         {
//             var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
//             
//             if (element.TryGetProperty("extension_pages", out var extPages))
//                 csp.ExtensionPages = extPages.GetString();
//                 
//             if (element.TryGetProperty("sandboxed_pages", out var sandboxPages))
//                 csp.SandboxedPages = sandboxPages.GetString();
//                 
//             if (element.TryGetProperty("isolated_world", out var isolatedWorld))
//                 csp.IsolatedWorld = isolatedWorld.GetString();
//         }
//         else if (reader.TokenType == JsonTokenType.Null)
//         {
//             return csp;
//         }
//
//         return csp;
//     }
//
//     public override void Write(Utf8JsonWriter writer, ContentSecurityPolicy value, JsonSerializerOptions options)
//     {
//         if (!string.IsNullOrEmpty(value.Policy))
//         {
//             writer.WriteStringValue(value.Policy);
//         }
//         else
//         {
//             writer.WriteStartObject();
//             if (!string.IsNullOrEmpty(value.ExtensionPages))
//                 writer.WriteString("extension_pages", value.ExtensionPages);
//             if (!string.IsNullOrEmpty(value.SandboxedPages))
//                 writer.WriteString("sandboxed_pages", value.SandboxedPages);
//             if (!string.IsNullOrEmpty(value.IsolatedWorld))
//                 writer.WriteString("isolated_world", value.IsolatedWorld);
//             writer.WriteEndObject();
//         }
//     }
// }
//
// // Converter for Author (handles string or object)
// public class AuthorConverter : JsonConverter<Author>
// {
//     public override Author Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//     {
//         var author = new Author();
//
//         if (reader.TokenType == JsonTokenType.String)
//         {
//             author.Name = reader.GetString();
//         }
//         else if (reader.TokenType == JsonTokenType.StartObject)
//         {
//             var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
//             
//             if (element.TryGetProperty("name", out var name))
//                 author.Name = name.GetString();
//                 
//             if (element.TryGetProperty("email", out var email))
//                 author.Email = email.GetString();
//         }
//         else if (reader.TokenType == JsonTokenType.Null)
//         {
//             return author;
//         }
//
//         return author;
//     }
//
//     public override void Write(Utf8JsonWriter writer, Author value, JsonSerializerOptions options)
//     {
//         if (!string.IsNullOrEmpty(value.Name) && string.IsNullOrEmpty(value.Email))
//         {
//             writer.WriteStringValue(value.Name);
//         }
//         else
//         {
//             writer.WriteStartObject();
//             if (!string.IsNullOrEmpty(value.Name))
//                 writer.WriteString("name", value.Name);
//             if (!string.IsNullOrEmpty(value.Email))
//                 writer.WriteString("email", value.Email);
//             writer.WriteEndObject();
//         }
//     }
// }
//
// // Enhanced WebAccessibleResourcesConverter
// public class WebAccessibleResourcesConverter : JsonConverter<List<WebAccessibleResource>>
// {
//     public override List<WebAccessibleResource> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//     {
//         var outList = new List<WebAccessibleResource>();
//
//         if (reader.TokenType == JsonTokenType.Null)
//             return outList;
//
//         var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
//
//         if (element.ValueKind != JsonValueKind.Array) 
//             return outList;
//
//         foreach (var item in element.EnumerateArray())
//         {
//             if (item.ValueKind == JsonValueKind.String)
//             {
//                 outList.Add(new WebAccessibleResource { Resources = new List<string> { item.GetString()! } });
//             }
//             else if (item.ValueKind == JsonValueKind.Object)
//             {
//                 var war = new WebAccessibleResource();
//
//                 if (item.TryGetProperty("resources", out var resources))
//                 {
//                     var list = new List<string>();
//                     if (resources.ValueKind == JsonValueKind.Array)
//                     {
//                         foreach (var r in resources.EnumerateArray())
//                             if (r.ValueKind == JsonValueKind.String) 
//                                 list.Add(r.GetString()!);
//                     }
//                     else if (resources.ValueKind == JsonValueKind.String)
//                     {
//                         list.Add(resources.GetString()!);
//                     }
//                     war.Resources = list;
//                 }
//
//                 if (item.TryGetProperty("matches", out var matches))
//                 {
//                     var list = new List<string>();
//                     if (matches.ValueKind == JsonValueKind.Array)
//                     {
//                         foreach (var m in matches.EnumerateArray())
//                             if (m.ValueKind == JsonValueKind.String) 
//                                 list.Add(m.GetString()!);
//                     }
//                     else if (matches.ValueKind == JsonValueKind.String)
//                     {
//                         list.Add(matches.GetString()!);
//                     }
//                     war.Matches = list;
//                 }
//
//                 if (item.TryGetProperty("extension_ids", out var extIds))
//                 {
//                     var list = new List<string>();
//                     if (extIds.ValueKind == JsonValueKind.Array)
//                     {
//                         foreach (var e in extIds.EnumerateArray())
//                             if (e.ValueKind == JsonValueKind.String) 
//                                 list.Add(e.GetString()!);
//                     }
//                     else if (extIds.ValueKind == JsonValueKind.String)
//                     {
//                         list.Add(extIds.GetString()!);
//                     }
//                     war.ExtensionIds = list;
//                 }
//
//                 if (item.TryGetProperty("use_dynamic_url", out var useDynamicUrl))
//                 {
//                     if (useDynamicUrl.ValueKind == JsonValueKind.True)
//                         war.UseDynamicUrl = true;
//                     else if (useDynamicUrl.ValueKind == JsonValueKind.False)
//                         war.UseDynamicUrl = false;
//                 }
//
//                 outList.Add(war);
//             }
//         }
//
//         return outList;
//     }
//
//     public override void Write(Utf8JsonWriter writer, List<WebAccessibleResource> value, JsonSerializerOptions options)
//     {
//         JsonSerializer.Serialize(writer, value, options);
//     }
// }
//
// public class LocalizedMessages
// {
//     [JsonExtensionData]
//     public Dictionary<string, JsonElement>? Messages { get; set; }
//
//     public string? GetMessage(string key)
//     {
//         if (Messages == null) return null;
//         
//         if (Messages.TryGetValue(key, out var element))
//         {
//             if (element.ValueKind == JsonValueKind.Object && 
//                 element.TryGetProperty("message", out var messageElement))
//             {
//                 return messageElement.GetString();
//             }
//             else if (element.ValueKind == JsonValueKind.String)
//             {
//                 return element.GetString();
//             }
//         }
//         return null;
//     }
// }
//
// public abstract class ExtensionManifestBase
// {
//     [JsonPropertyName("manifest_version")]
//     public int ManifestVersion { get; set; }
//
//     [JsonPropertyName("name")]
//     public string? Name { get; set; }
//
//     [JsonPropertyName("short_name")]
//     public string? ShortName { get; set; }
//
//     [JsonPropertyName("version")]
//     public string? Version { get; set; }
//
//     [JsonPropertyName("version_name")]
//     public string? VersionName { get; set; }
//
//     [JsonPropertyName("description")]
//     public string? Description { get; set; }
//
//     [JsonPropertyName("default_locale")]
//     public string? DefaultLocale { get; set; }
//
//     [JsonPropertyName("update_url")]
//     public string? UpdateUrl { get; set; }
//
//     [JsonPropertyName("minimum_chrome_version")]
//     public string? MinimumChromeVersion { get; set; }
//
//     [JsonPropertyName("homepage_url")]
//     public string? HomepageUrl { get; set; }
//
//     [JsonPropertyName("offline_enabled")]
//     public bool? OfflineEnabled { get; set; }
//
//     [JsonExtensionData]
//     public Dictionary<string, JsonElement>? ExtraData { get; set; }
//
//     [JsonIgnore]
//     public string? ExtensionFolderPath { get; set; }
//
//     public T? GetExtraValue<T>(string key)
//     {
//         if (ExtraData == null) return default;
//         if (!ExtraData.TryGetValue(key, out var el)) return default;
//         try
//         {
//             return JsonSerializer.Deserialize<T>(el.GetRawText(), DefaultOptions);
//         }
//         catch
//         {
//             return default;
//         }
//     }
//
//     // Get localized version of a field value
//     public string? GetLocalizedValue(string? value, string language = "en")
//     {
//         if (string.IsNullOrEmpty(value) || !IsLocalizedString(value))
//             return value;
//
//         return ExtractLocalizedString(value, language) ?? value;
//     }
//
//     // Get localized name
//     public string? GetLocalizedName(string language = "en")
//     {
//         return GetLocalizedValue(Name, language);
//     }
//
//     // Get localized short name
//     public string? GetLocalizedShortName(string language = "en")
//     {
//         return GetLocalizedValue(ShortName, language);
//     }
//
//     // Get localized description
//     public string? GetLocalizedDescription(string language = "en")
//     {
//         return GetLocalizedValue(Description, language);
//     }
//
//     // Check if a string is a localization key
//     private static bool IsLocalizedString(string? value)
//     {
//         return !string.IsNullOrEmpty(value) && 
//                value.StartsWith("__MSG_") && 
//                value.EndsWith("__");
//     }
//
//     private string? ExtractLocalizedString(string localizedKey, string language = "en")
//     {
//         if (string.IsNullOrEmpty(ExtensionFolderPath) || !IsLocalizedString(localizedKey))
//             return null;
//
//         var messageKey = localizedKey.Substring(6, localizedKey.Length - 8);
//         
//         var localizedMessage = GetLocalizedMessage(messageKey, language);
//         
//         if (localizedMessage == null && !string.IsNullOrEmpty(DefaultLocale) && DefaultLocale != language)
//         {
//             localizedMessage = GetLocalizedMessage(messageKey, DefaultLocale);
//         }
//         
//         if (localizedMessage == null && language != "en" && DefaultLocale != "en")
//         {
//             localizedMessage = GetLocalizedMessage(messageKey, "en");
//         }
//
//         return localizedMessage;
//     }
//
//     private string? GetLocalizedMessage(string messageKey, string language)
//     {
//         if (string.IsNullOrEmpty(ExtensionFolderPath))
//             return null;
//
//         var messagesPath = Path.Combine(ExtensionFolderPath, "_locales", language, "messages.json");
//         
//         if (!File.Exists(messagesPath))
//             return null;
//
//         try
//         {
//             var json = File.ReadAllText(messagesPath);
//             var messages = JsonSerializer.Deserialize<LocalizedMessages>(json, DefaultOptions);
//             return messages?.GetMessage(messageKey);
//         }
//         catch
//         {
//             return null;
//         }
//     }
//
//     protected static JsonSerializerOptions DefaultOptions => new JsonSerializerOptions
//     {
//         PropertyNameCaseInsensitive = true,
//         AllowTrailingCommas = true,
//         ReadCommentHandling = JsonCommentHandling.Skip,
//         TypeInfoResolver = new DefaultJsonTypeInfoResolver()
//     };
// }
//
// public static class ExtensionManifestParser
// {
//     private static JsonSerializerOptions Options
//     {
//         get
//         {
//             var o = new JsonSerializerOptions
//             {
//                 PropertyNameCaseInsensitive = true,
//                 AllowTrailingCommas = true,
//                 ReadCommentHandling = JsonCommentHandling.Skip,
//                 TypeInfoResolver = new DefaultJsonTypeInfoResolver()
//             };
//             o.Converters.Add(new WebAccessibleResourcesConverter());
//             o.Converters.Add(new IconsConverter());
//             o.Converters.Add(new ContentSecurityPolicyConverter());
//             o.Converters.Add(new AuthorConverter());
//             return o;
//         }
//     }
//
//     public static ExtensionManifestBase Parse(string json, string? extensionFolderPath = null)
//     {
//         using var doc = JsonDocument.Parse(json);
//         var root = doc.RootElement;
//
//         int manifestVersion = 2;
//         if (root.TryGetProperty("manifest_version", out var mv))
//         {
//             if (mv.ValueKind == JsonValueKind.Number && mv.TryGetInt32(out var iv)) 
//                 manifestVersion = iv;
//             else if (mv.ValueKind == JsonValueKind.String && int.TryParse(mv.GetString(), out var sval)) 
//                 manifestVersion = sval;
//         }
//
//         ExtensionManifestBase result;
//         try
//         {
//             if (manifestVersion >= 3)
//             {
//                 var v3 = JsonSerializer.Deserialize<ExtensionManifestV3>(json, Options);
//                 result = v3 ?? throw new InvalidOperationException("Failed to deserialize as V3.");
//             }
//             else
//             {
//                 var v2 = JsonSerializer.Deserialize<ExtensionManifestV2>(json, Options);
//                 result = v2 ?? throw new InvalidOperationException("Failed to deserialize as V2.");
//             }
//         }
//         catch (JsonException ex)
//         {
//             throw new InvalidOperationException($"Failed to parse extension manifest: {ex.Message}", ex);
//         }
//
//         result.ExtensionFolderPath = extensionFolderPath;
//         return result;
//     }
//
//     public static async Task<ExtensionManifestBase> ParseFromFileAsync(string path)
//     {
//         var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
//         var extensionFolderPath = Path.GetDirectoryName(path);
//         return Parse(json, extensionFolderPath);
//     }
// }