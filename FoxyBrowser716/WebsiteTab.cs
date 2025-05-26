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
	
	public readonly WebView2 TabCore;
	public readonly int TabId;
	public string Title { get; private set; }

	public readonly Task SetupTask;
		
	private static int _tabCounter;
	private InstanceManager _instance;

	public Dictionary<string, (string Name, string Path)> Extensions = [];

	public WebsiteTab(string url, CoreWebView2Environment websiteEnvironment, InstanceManager instance)
	{
		var webView = new WebView2();
		webView.Visibility = Visibility.Collapsed;
			
		_instance = instance;
		
		TabCore = webView;
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

		TabCore.CoreWebView2.NewWindowRequested += (_,e) =>
		{
			e.Handled = true;
			NewTabRequested?.Invoke(e.Uri);
		};

		TabCore.CoreWebView2.DownloadStarting += 
			async (_,e) => await OnDownload(e);

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

	#region ExtensionDownloadingStuff
	private async Task OnDownload(CoreWebView2DownloadStartingEventArgs e)
	{
		// we only care about downloads from the chrome webstore for now, skip anything else:
		if (!TabCore.CoreWebView2.Source.Contains("chromewebstore.google.com")) return;
		
        var downloadUrl = e.DownloadOperation.Uri;
        
        string extensionId = null;
        
        // working method for extracting extension ID
        if (downloadUrl.Contains("x=id%3D") || downloadUrl.Contains("x=id="))
        {
            var uri = new Uri(downloadUrl);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            var xParam = queryParams["x"];
            
            if (!string.IsNullOrEmpty(xParam))
            {
                var xParamParts = xParam.Split(["%26", "&"], StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in xParamParts)
                {
                    if (part.StartsWith("id="))
                    {
                        extensionId = part.Substring(3);
                        break;
                    }
                }
            }
        }
        
        //TODO: remove these unneeded method later after testing
        /*if (string.IsNullOrEmpty(extensionId) && downloadUrl.Contains("chrome.google.com/webstore/detail/"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(downloadUrl, 
                @"chrome\.google\.com/webstore/detail/[^/]+/([a-z]{32})");
            if (match.Success && match.Groups.Count > 1)
            {
                extensionId = match.Groups[1].Value;
            }
        }*/
        
        /*if (string.IsNullOrEmpty(extensionId))
        {
            var match = System.Text.RegularExpressions.Regex.Match(downloadUrl, 
                @"[a-z]{32}");
            if (match.Success)
            {
                extensionId = match.Value;
            }
        }*/
        
        if (!string.IsNullOrEmpty(extensionId))
        {
            e.Cancel = true; 
            
            await DownloadAndInstallExtension(extensionId);
            
            //TODO: let the user know about a new extension download
            
            //TODO: let the instance know about a new extension
        }
	}
	private async Task DownloadAndInstallExtension(string extensionId)
	{
	    try
	    {
	        using var handler = new HttpClientHandler
	        {
	            AllowAutoRedirect = true,
	            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
	        };
	        
	        using var client = new HttpClient(handler);
	        
	        client.DefaultRequestHeaders.Add("User-Agent", 
	            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
	        client.DefaultRequestHeaders.Add("Accept", "*/*");
	        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
	        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
	        client.DefaultRequestHeaders.Add("Connection", "keep-alive");

	        var crxUrl = $"https://clients2.google.com/service/update2/crx?response=redirect&acceptformat=crx2,crx3&prodversion=100.0&x=id%3D{extensionId}%26installsource%3Dondemand%26uc";
	        
	        var response = await client.GetAsync(crxUrl);
	        
	        if (!response.IsSuccessStatusCode)
	        {
	            crxUrl = $"https://chrome.google.com/webstore/download/{extensionId}?acceptformat=crx3&installsource=ondemand";
	            response = await client.GetAsync(crxUrl);
	        }
	        
	        if (response.IsSuccessStatusCode)
	        {
	            var crxBytes = await response.Content.ReadAsByteArrayAsync();
	            
	            if (crxBytes.Length < 100) // Suspiciously small file, likely an error
	            {
	                throw new Exception($"Downloaded file too small ({crxBytes.Length} bytes), likely not a valid extension");
	            }
	            
	            var folder = Path.Combine(_instance.ExtensionFolder, extensionId);
	            Directory.CreateDirectory(folder);
	            
	            using var ms = new MemoryStream(crxBytes);
	            
	            try
	            {
	                var crxFilePath = Path.Combine(folder, $"{extensionId}.crx");
	                await File.WriteAllBytesAsync(crxFilePath, crxBytes);

                    UnpackCrxStream(ms, folder);
	                
	                var ext = await TabCore.CoreWebView2.Profile.AddBrowserExtensionAsync(folder);
	                if (ext != null)
	                {
	                    Extensions.Add(ext.Id, (ext.Name, folder));
	                    MessageBox.Show($"Extension '{ext.Name}' installed successfully!");
	                }
	                else
	                {
		                throw new Exception("Extension could not be loaded after download");
	                }
	            }
	            catch (Exception ex)
	            {
		            throw new Exception($"Error unpacking extension: {ex.Message}");
	            }
	        }
	        else
	        {
		        throw new Exception($"Failed to download extension: HTTP {response.StatusCode}");
	        }
	    }
	    catch (Exception ex)
	    {
		    throw new Exception($"Error downloading extension: {ex.Message}");
	    }
	}

    private static void UnpackCrxStream(Stream crxStream, string outFolder)
	{
	    // Reset stream position to beginning
	    crxStream.Position = 0;
	    
	    using var br = new BinaryReader(crxStream, Encoding.UTF8, leaveOpen: true);
	    
	    // Read CRX header signature ("Cr24")
	    var signature = new string(br.ReadChars(4));
	    if (signature != "Cr24")
	    {
	        throw new InvalidDataException("Invalid CRX header signature. Expected 'Cr24'.");
	    }
	    
	    // Read version
	    var version = br.ReadUInt32();
	    Console.WriteLine($"CRX version: {version}");
	    
	    // Handle different CRX versions
	    if (version == 2)
	    {
	        // CRX2 format:
	        // - 4 bytes: header size
	        // - header size bytes: public key
	        // - header size bytes: signature
	        var headerSize = br.ReadUInt32();
	        
	        // Skip public key and signature
	        br.ReadBytes((int)headerSize * 2);
	    }
	    else if (version == 3)
	    {
	        // CRX3 format:
	        // - 4 bytes: header size
	        // - 4 bytes: CRX3 header size
	        // - ... more complex header with multiple sections
	        var headerSize = br.ReadUInt32();
	        
	        // Skip the entire header
	        br.ReadBytes((int)headerSize);
	    }
	    else
	    {
	        throw new InvalidDataException($"Unsupported CRX version: {version}");
	    }
	    
	    // At this point, the remainder of the stream should be a valid ZIP archive
	    
	    // Create a new memory stream containing just the ZIP data
	    var zipDataSize = crxStream.Length - crxStream.Position;
	    var zipData = new byte[zipDataSize];
	    crxStream.Read(zipData, 0, (int)zipDataSize);
	    
	    using var zipStream = new MemoryStream(zipData);
	    try
	    {
	        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
	        
	        // First ensure the output directory is clean
	        if (Directory.Exists(outFolder))
	        {
	            // Keep the CRX file but delete other contents
	            var filesToDelete = Directory.GetFiles(outFolder)
	                .Where(f => !f.EndsWith(".crx", StringComparison.OrdinalIgnoreCase));
	            
	            foreach (var file in filesToDelete)
	            {
	                File.Delete(file);
	            }
	            
	            // Delete subdirectories
	            foreach (var dir in Directory.GetDirectories(outFolder))
	            {
	                Directory.Delete(dir, true);
	            }
	        }
	        else
	        {
	            Directory.CreateDirectory(outFolder);
	        }
	        
	        // Extract the archive
	        archive.ExtractToDirectory(outFolder, overwriteFiles: true);
	        
	        // For debugging: Write manifest.json content to log
	        var manifestPath = Path.Combine(outFolder, "manifest.json");
	        if (File.Exists(manifestPath))
	        {
	            Console.WriteLine($"Extension manifest found: {manifestPath}");
	            // You could log the manifest content here if needed
	        }
	        else
	        {
	            Console.WriteLine($"Warning: No manifest.json found in extracted extension");
	        }
	    }
	    catch (Exception ex)
	    {
	        Console.WriteLine($"Error extracting ZIP data: {ex.Message}");
	        
	        // For debugging: Save the raw ZIP data to analyze
	        var debugZipPath = Path.Combine(outFolder, "debug_zip_data.zip");
	        File.WriteAllBytes(debugZipPath, zipData);
	        
	        throw; // Re-throw to handle in the calling method
	    }
	}
	#endregion
}