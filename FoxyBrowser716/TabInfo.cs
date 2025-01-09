using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxyBrowser716;

public record TabInfo
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public DateTime Added { get; init; }
	
	[JsonIgnore]
	public ImageSource Image  { get; init; }

	private string? Base64Image;

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
    /// Does what it says.
    /// Images are saved as a base64 string
    /// </summary>
    /// <param name="filePath">full path of the file to create or overwrite</param>
    /// <param name="tabs">collection of tabs to save</param>
    /// <param name="retryCount">Internal Use Only</param>
    public static async Task SaveTabs(string filePath, IEnumerable<TabInfo> tabs, int retryCount = 0)
    {
        try
        {
            var saveableTabs = tabs.Select(tab => new TabInfo
            {
                Url = tab.Url,
                Title = tab.Title,
                Added = tab.Added,
                Base64Image = tab.Image != null ? ImageSourceToBase64(tab.Image) : null
            }).ToList();

            var json = JsonSerializer.Serialize(saveableTabs, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            if (retryCount < 3)
                await SaveTabs(filePath, tabs, retryCount + 1);
            else
                MessageBox.Show(ex.Message, $"Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Does what it says.
    /// </summary>
    /// <param name="filePath">full path of the file to create or overwrite.</param>
    /// <param name="tabs">an IList to add tabs to.</param>
    /// <param name="retryCount">Internal Use Only</param>
    public static async Task TryLoadTabs(string filePath, IList<TabInfo> tabs, int retryCount = 0)
    {
        var e = true;
        try
        {
            if (!File.Exists(filePath)) return;

            var json = await File.ReadAllTextAsync(filePath);
            var loadedTabs = JsonSerializer.Deserialize<List<TabInfo>>(json);

            if (loadedTabs != null)
            {
                tabs.Clear();
                foreach (var tab in loadedTabs)
                {
                    tabs.Add(new TabInfo
                    {
                        Url = tab.Url,
                        Title = tab.Title,
                        Added = tab.Added,
                        Image = tab.Base64Image != null ? Base64ToImageSource(tab.Base64Image) : null
                    });
                }
            }
        }
        catch (Exception ex)
        {
            if (retryCount < 3)
                await TryLoadTabs(filePath, tabs, retryCount + 1);
            else
                MessageBox.Show(ex.Message, $"Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}