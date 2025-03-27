using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using WpfAnimatedGif;

namespace FoxyBrowser716;

public class WebsiteTab
{
	private Image _image = new();
	public Image Icon
	{
		get => new()
		{
			Source = _image.Source,
			Stretch = _image.Stretch,
		};
		private set => _image = value;
	}
	
	public readonly WebView2 TabCore;
	public readonly int TabId;
	public string Title { get; private set; }

	public readonly Task SetupTask;
		
	private static int _tabCounter;

	public WebsiteTab(string url)
	{
		var webView = new WebView2();
		webView.Visibility = Visibility.Collapsed;
			
		TabCore = webView;
		TabCore.DefaultBackgroundColor = System.Drawing.Color.FromArgb(0, 0, 0);
		TabId = Interlocked.Increment(ref _tabCounter); // thread safe version of _tabCounter++

		// Display a loading animation while fetching the favicon
		var image = new Image();
		var gifSource = new BitmapImage(new Uri("pack://application:,,,/Resources/NoiceLoadingIconBlack.gif"));
		ImageBehavior.SetAnimatedSource(image, gifSource);
		Icon = image;
		Title = "Loading...";
		
		SetupTask = Initialize(url);
	}

	public event Action UrlChanged;
	public event Action ImageChanged;
	public event Action TitleChanged;
	public event Action<string> NewTabRequested;
	
	private async Task Initialize(string url)
	{
		await TabCore.EnsureCoreWebView2Async(TabManager.WebsiteEnvironment);
			
		TabCore.CoreWebView2.Settings.AreDevToolsEnabled = true;
			
		await LoadExtensions();
		
		TabCore.SourceChanged += (_, _) =>
		{
			UrlChanged?.Invoke();
		};

		TabCore.NavigationCompleted += (_, _) =>
		{
			Title = TabCore.CoreWebView2.DocumentTitle;
			TitleChanged?.Invoke();
		};
		
		TabCore.CoreWebView2.FaviconChanged += async (_, _) => await RefreshImage();

		TabCore.CoreWebView2.NewWindowRequested += (_,e) =>
		{
			e.Handled = true;
			NewTabRequested?.Invoke(e.Uri);
		};
		
		try
		{
			TabCore.Source = new Uri(url);
		}
		catch (Exception exception)
		{
			TabCore.Source = new Uri($"https://www.google.com/search?q={url}");
		}
	}
	
	private async Task LoadExtensions()
	{
	    try
	    {
	        // Define the extension folder path relative to the application directory
	        var extensionsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extensions");
	
	        // Check if the directory exists, if not, create it (just in case)
	        if (!Directory.Exists(extensionsFolder))
	        {
	            Directory.CreateDirectory(extensionsFolder);
	        }
	
	        async Task<bool> IsExtension(string path)
	        {
		        var manifestFile = Directory.GetFiles(path, "manifest.json", SearchOption.TopDirectoryOnly)
			        .FirstOrDefault();
	
		        if (manifestFile != null)
		        {
			        await TabCore.CoreWebView2.Profile.AddBrowserExtensionAsync(path);
		        }
	
		        return manifestFile != null;
	        }
	        
	        // Find all subfolders within the "extensions" directory
	        foreach (var subfolder in Directory.GetDirectories(extensionsFolder))
	        {
	            try
	            {
		            // Check if the subfolder contains a manifest.json file
		            if (await IsExtension(subfolder)) continue;
		            if (Directory.GetDirectories(subfolder).Length == 1)
			            await IsExtension(Directory.GetDirectories(subfolder)[0]);
	                else
		                throw new FileNotFoundException("Manifest.json not found in " + subfolder);
	            }
	            catch (Exception ex)
	            {
	                // If any error occurs loading an extension, show a message box for now
	                MessageBox.Show(
	                    $"Failed to load extension in folder: {subfolder}\nError: {ex.Message}",
	                    "Extension Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
	            }
	        }
	    }
	    catch (Exception ex)
	    {
	        // Catch and display errors in initializing or accessing the extensions folder
	        MessageBox.Show(
	            $"Failed to initialize extensions loader.\nError: {ex.Message}",
	            "Extensions Error", MessageBoxButton.OK, MessageBoxImage.Error);
	    }
	}

	private async Task RefreshImage()
	{
		ImageChanged?.Invoke();

		try
		{
			await using var stream = await TabCore.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
			if (stream != null && stream.Length > 0)
			{
				var bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.StreamSource = stream;
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.EndInit();
				stream.Close();

				
				
				Icon = new Image { Source = bitmap };
			}
		}
		catch
		{
			// ignored
		}

		ImageChanged?.Invoke();
	}

	
	private static async Task<ImageSource> GetImageSourceFromStreamAsync(Stream stream)
	{
		var bitmap = new BitmapImage();
		await using (stream)
		{
			bitmap.BeginInit();
			bitmap.StreamSource = stream;
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();
		}
		return bitmap;
	}

	private static BitmapSource CreateCircleWithLetter(int width, int height, string letter, Brush circleBrush,
		Brush textBrush)
	{
		var drawingVisual = new DrawingVisual();
		using (var dc = drawingVisual.RenderOpen())
		{
			dc.DrawEllipse(circleBrush, null, new Point(width / 2.0, height / 2.0), width / 2.0, height / 2.0);

			var formattedText = new FormattedText(
				letter,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				new Typeface("Arial"),
				Math.Min(width, height) / 2.0,
				textBrush,
				VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

			var textPosition = new Point(
				(width - formattedText.Width) / 2,
				(height - formattedText.Height) / 2);
			dc.DrawText(formattedText, textPosition);
		}

		var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
		bitmap.Render(drawingVisual);

		return bitmap;
	}
}