using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using WpfAnimatedGif;
using Color = System.Drawing.Color;

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
	
	public readonly WebView2CompositionControl TabCore;
	public readonly int TabId;
	public string Title { get; private set; }

	public readonly Task SetupTask;
		
	private static int _tabCounter;
	private InstanceManager _instance;

	public Dictionary<string, (string Name, string Path)> Extensions = [];

	public WebsiteTab(string url, CoreWebView2Environment websiteEnvironment, InstanceManager instance)
	{
		TabCore = new WebView2CompositionControl();
		TabCore.Visibility = Visibility.Collapsed;
		
		TabCore.AllowExternalDrop = true;
		TabCore.AllowDrop = true;
		TabCore.UseLayoutRounding = true;
		_instance = instance;
		
		TabCore.DefaultBackgroundColor = Color.Black;
		TabId = Interlocked.Increment(ref _tabCounter); // thread safe version of _tabCounter++
		
		var image = new Image();
		// var gifSource = new BitmapImage(new Uri("pack://application:,,,/Resources/NoiceLoadingIconBlack.gif"));
		// ImageBehavior.SetAnimatedSource(image, gifSource);
		Icon = image;
		Title = "Loading...";
		
		SetupTask = Initialize(url, websiteEnvironment);
	}

	public event Action UrlChanged;
	public event Action ImageChanged;
	public event Action TitleChanged;
	public event Action<string> NewTabRequested;
	
	private async Task Initialize(string url, CoreWebView2Environment websiteEnvironment)
	{
		await TabCore.EnsureCoreWebView2Async(websiteEnvironment);
		
		var extensionLoadTask = LoadExtensions();
		TabCore.AllowExternalDrop = true;
		TabCore.AllowDrop = true;
		TabCore.CoreWebView2.DefaultDownloadDialogCornerAlignment = CoreWebView2DefaultDownloadDialogCornerAlignment.TopLeft;
		TabCore.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
		TabCore.CoreWebView2.Profile.IsPasswordAutosaveEnabled = true;
		TabCore.CoreWebView2.Profile.IsGeneralAutofillEnabled = true;
			
		TabCore.CoreWebView2.Settings.AreDevToolsEnabled = true;
		
		TabCore.SourceChanged += (_, _) =>
		{
			UrlChanged?.Invoke();
		};

		TabCore.NavigationCompleted += (_, _) =>
		{
			Title = TabCore.CoreWebView2.DocumentTitle;
			TitleChanged?.Invoke();
			TabCore.DefaultBackgroundColor = Color.White;
		};
		
		TabCore.CoreWebView2.FaviconChanged += async (_, _) => await RefreshImage();

		/*TabCore.CoreWebView2.NewWindowRequested += (_,e) =>
		{
			e.Handled = true;
			NewTabRequested?.Invoke(e.Uri);
		};*/

		TabCore.CoreWebView2.DownloadStarting +=
			async (_, e) =>
			{
				// we only care about downloads from the chrome webstore for now,
				// skip anything else (aka, let the normal download handler take over):
				if (TabCore.CoreWebView2.Source.Contains("chromewebstore.google.com"))
					await _instance.ExtractIdAndAddExtension(e);
			};

		await extensionLoadTask;
		
		try
		{
			TabCore.Source = new Uri(url);
		}
		catch
		{
			TabCore.Source = new Uri($"https://www.google.com/search?q={Uri.EscapeDataString(url)}");
		}
	}
	
	private async Task LoadExtensions()
	{
        if (!Directory.Exists(_instance.ExtensionFolder))
        {
            Directory.CreateDirectory(_instance.ExtensionFolder);
        }

        async Task<bool> IsExtension(string path)
        {
	        var manifestFile = Directory.GetFiles(path, "manifest.json", SearchOption.TopDirectoryOnly)
		        .FirstOrDefault();

	        if (manifestFile != null)
	        {
		        var ext = await TabCore.CoreWebView2.Profile.AddBrowserExtensionAsync(path);
		        if (ext != null)
		        {
			        Extensions.Add(ext.Id, (ext.Name, path));
			        //await TabCore.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(MainWindow.GetScript(MainWindow.FindPopupHtml(manifestFile, path)));
		        }
	        }

	        return manifestFile != null;
        }
        
        foreach (var subfolder in Directory.GetDirectories(_instance.ExtensionFolder))
        {
            if (await IsExtension(subfolder)) continue;
            if (Directory.GetDirectories(subfolder).Length == 1)
	            await IsExtension(Directory.GetDirectories(subfolder)[0]); //TODO: loop through all + what if empty folder?
            //TODO: find a proper way to handle this:
            /*else
                throw new FileNotFoundException("Manifest.json not found in " + subfolder);*/
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
}