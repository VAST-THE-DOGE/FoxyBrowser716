using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxyBrowser716;

public record TabInfo
{
    public required string Url { get; init; }
    public required string Title { get; init; }
    public DateTime Added { get; init; }

    public string? Base64Image { get; init; } // Make Base64Image public for serialization
    
    [JsonIgnore]
    private Image _image = new();
    
    [JsonIgnore]

    public Image Image
    {
        get => new()
        {
            Source = _image.Source,
            Stretch = _image.Stretch,
        };
        init => _image = value;
    }

    private static string ImageSourceToBase64(ImageSource image)
    {
        if (image is BitmapSource bitmap)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return Convert.ToBase64String(stream.ToArray());
        }
        return string.Empty;
    }

    private static ImageSource Base64ToImageSource(string base64)
    {
        var imageData = Convert.FromBase64String(base64);
        using var stream = new MemoryStream(imageData);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return decoder.Frames.First();
    }

    /// <summary>
    /// Saves tabs to a file. Images are saved as a base64 string.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static async Task SaveTabs(string filePath, IEnumerable<TabInfo> tabs, int retryCount = 0)
    {
        var tabInfos = tabs as TabInfo[] ?? tabs.ToArray();
        try
        {
            // Prepare a saveable collection of tabs
            var saveableTabs = tabInfos.Select(tab => new TabInfo
            {
                Url = tab.Url,
                Title = tab.Title,
                Added = tab.Added,
                Base64Image = tab.Image?.Source is { } source ? ImageSourceToBase64(source) : null
            }).ToList();

            // Serialize the tab list to JSON
            var json = JsonSerializer.Serialize(saveableTabs, Options);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception _)
        {
            if (retryCount < 3)
                await SaveTabs(filePath, tabInfos, retryCount + 1);
            else
                throw;
        }
    }

    /// <summary>
    /// Loads tabs from a file and restores their image representations.
    /// </summary>
    public static async Task<List<TabInfo>> TryLoadTabs(string filePath, int retryCount = 0)
    {
        var tabs = new List<TabInfo>();
        try
        {
            if (!File.Exists(filePath)) return [];

            // Read JSON file
            var json = await File.ReadAllTextAsync(filePath);

            // Deserialize JSON into a collection of TabInfo
            var loadedTabs = JsonSerializer.Deserialize<List<TabInfo>>(json);

            if (loadedTabs != null)
            {
                foreach (var tab in loadedTabs)
                {
                    tabs.Add(new TabInfo
                    {
                        Url = tab.Url,
                        Title = tab.Title,
                        Added = tab.Added,
                        Base64Image = tab.Base64Image,
                        Image = tab.Base64Image != null ? new Image
                        {
                            Source = Base64ToImageSource(tab.Base64Image),                            
                            Width = 24,
                            Height = 24,
                            Margin = new Thickness(1),
                            Stretch = Stretch.Uniform
                        } : new Image()
                    });
                }

                return tabs;
            }
        }
        catch (Exception _)
        {
            if (retryCount < 3)
                return await TryLoadTabs(filePath, retryCount + 1);
            else
                throw;
        }
        return [];
    }
}