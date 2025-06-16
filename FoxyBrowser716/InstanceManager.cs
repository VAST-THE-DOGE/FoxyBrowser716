using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Windows;
using FoxyBrowser716.Settings;
using Microsoft.Web.WebView2.Core;

namespace FoxyBrowser716;

public class InstanceManager
{
	public readonly WebsiteInfoList PinInfo = new();
	public readonly WebsiteInfoList BookmarkInfo = new();
	public readonly BrowserSettingsManager SettingsManager = new();
	
	public List<BrowserApplicationWindow> BrowserWindows = [];
	
	public BrowserApplicationWindow? CurrentBrowserWindow;

	public readonly string InstanceFolder;
	public readonly string ExtensionFolder;
	
	public string InstanceName {get; private set; }

	public readonly bool PrimaryInstance;
	
	public event Action<InstanceManager> Focused;
	
	public InstanceManager(string name)
	{
		InstanceName = name;
		PrimaryInstance = name == "Default";
		InstanceFolder = Path.Combine(InfoGetter.InstanceFolder, InstanceName);
		ExtensionFolder = Path.Combine(InstanceFolder, "extensions");

		if (!Directory.Exists(InstanceFolder)) Directory.CreateDirectory(InstanceFolder);
	}

	public enum BrowserWindowState
	{
		Minimized,
		Normal,
		Maximized,
		Borderless
	}

	public static BrowserWindowState StateFromWindow(BrowserApplicationWindow window)
	{
		switch (window.WindowState)
		{
			case WindowState.Minimized:
				return BrowserWindowState.Minimized;
			case WindowState.Normal:
				return BrowserWindowState.Normal;
			case WindowState.Maximized:
				return window.BorderlessFullscreen 
					? BrowserWindowState.Borderless 
					: BrowserWindowState.Maximized;
			default:
				return BrowserWindowState.Normal;
		}
	}

	public static void ApplyWindowState(BrowserWindowState windowState, BrowserApplicationWindow window)
	{
		switch (windowState)
		{
			case BrowserWindowState.Minimized:
				window.BorderlessFullscreen = false;
				window.WindowState = WindowState.Minimized;
				break;
			case BrowserWindowState.Normal:
				window.BorderlessFullscreen = false;
				window.WindowState = WindowState.Normal;
				break;
			case BrowserWindowState.Maximized:
				window.BorderlessFullscreen = false;
				window.WindowState = WindowState.Maximized;
				break;
			case BrowserWindowState.Borderless:
				window.BorderlessFullscreen = true;
				window.WindowState = WindowState.Normal;
				window.WindowState = WindowState.Maximized;
				break;
		}
	}
	
	public async Task<BrowserApplicationWindow> CreateWindow(string? url = null,
		Rect? startLocation = null, BrowserWindowState windowState = BrowserWindowState.Normal)
	{
		var newWindow = new BrowserApplicationWindow(this);
		
		if (startLocation is {Height: > 25, Width: > 50 } sl)
		{
			newWindow.Top = sl.Y;
			newWindow.Left = sl.X;
			newWindow.Height = sl.Height;
			newWindow.Width = sl.Width;
		}
		ApplyWindowState(windowState, newWindow);
		
		await newWindow.InitTask; 
		
		BrowserWindows.Add(newWindow);
		newWindow.GotFocus += (s, e) =>
		{
			if (s is BrowserApplicationWindow win)
			{
				CurrentBrowserWindow = win;
				Focused?.Invoke(this);
			}
		};
		newWindow.Closed += (w, _) => 
		{ 
			if (w is BrowserApplicationWindow baw)
				BrowserWindows.Remove(baw);
			else
				throw new InvalidOperationException(
					$"Cannot remove browser application window from instance '{InstanceName}', sender type mismatch.");
		};

		if (url != null)
		{
			newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url));
		}
		
		newWindow.Show();
		newWindow.Focus();
		return newWindow;
	}

	public async Task<BrowserApplicationWindow> CreateAndTransferTabToWindow(WebsiteTab tab, 
		Rect? startLocation = null, BrowserWindowState windowState = BrowserWindowState.Normal)
	{
		var newWindow = await CreateWindow(null, startLocation, windowState);
		newWindow.TabManager.SwapActiveTabTo(await newWindow.TabManager.TransferTab(tab));
		return newWindow;
	}
		
	public async Task Initialize()
	{
		await LoadData();
	}

	public async Task SaveData()
	{
		// note, WebsiteInfoList handles saving on add and remove on its own
		//TODO
	}

	public async Task LoadData()
	{
		await Task.WhenAll(
			PinInfo.LoadTabInfoFromJson(Path.Combine(InstanceFolder, "pins.json")),
			BookmarkInfo.LoadTabInfoFromJson(Path.Combine(InstanceFolder, "bookmarks.json"))
		);
		//TODO
	}

	public async Task RenameInstance()
	{
		//TODO
	}
	
	#region ExtensionDownloadingStuff
	private FoxyPopup? _popup;

	public async Task ExtractIdAndAddExtension(CoreWebView2DownloadStartingEventArgs e)
	{
		Application.Current.Dispatcher.Invoke(() =>
		{
			_popup = new FoxyPopup
			{
				Title = "Installing Extension",
				Subtitle = "Extracting extension ID...",
				ShowProgressbar = true,
			};
			_popup.Show();
		});
		
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
            
            await AddExtension(extensionId);
        }
        else
        {
	        Application.Current.Dispatcher.Invoke(() =>
	        {
		        _popup.Subtitle = "Failed to extract extension ID.\nThe extension has not been installed.";
		        _popup.ShowProgressbar = false;
		        _popup.SetButtons([new FoxyPopup.BottomButton(() => _popup.Close(), "OK")]);;
	        });
        }
	}
	public async Task AddExtension(string id)
	{
		Application.Current.Dispatcher.Invoke(() =>
		{
			if (_popup is not null) return;
			
			_popup = new FoxyPopup
			{
				Title = "Installing Extension",
				Subtitle = "",
				ShowProgressbar = true,
			};
			_popup.Show();
		});
		
		await DownloadAndInstallExtension(id);
            
        Application.Current.Dispatcher.Invoke(() =>
        {
            _popup.Subtitle = "Loading extension...";
        });
            
        var folder = Path.Combine(ExtensionFolder, id);
        await Task.WhenAll(BrowserWindows.Select(win => win.TabManager.GetAllTabs())
		        .SelectMany(tabs => tabs)
		        .Where(pair => pair.Value.TabCore.CoreWebView2 is not null)
		        .Select(t => t.Value.TabCore.CoreWebView2.Profile.AddBrowserExtensionAsync(folder))
	    );
            
        Application.Current.Dispatcher.Invoke(() =>
        {
            _popup.Subtitle = "Extension has been installed.";
            _popup.ShowProgressbar = false;
            _popup.SetButtons([new FoxyPopup.BottomButton(() => {
	            _popup.Close();
	            _popup = null;
            }, "OK")]);;
        }); 
	}

	private async Task DownloadAndInstallExtension(string extensionId)
	{
	    Application.Current.Dispatcher.Invoke(() =>
	    {
		    _popup.Subtitle = "Downloading crx file...";
	    });
	    
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
            
            if (crxBytes.Length < 100)
            {
                throw new Exception($"Downloaded file too small ({crxBytes.Length} bytes), likely not a valid extension");
            }
            
            Application.Current.Dispatcher.Invoke(() =>
            {
	            _popup.Subtitle = "Extracting to folder...";
            });
            var folder = Path.Combine(ExtensionFolder, extensionId);
            Directory.CreateDirectory(folder);
            
            using var ms = new MemoryStream(crxBytes);
            
            
	        var crxFilePath = Path.Combine(folder, $"{extensionId}.crx");
	        await File.WriteAllBytesAsync(crxFilePath, crxBytes);

	        UnpackCrxStream(ms, folder);
        }
        else
        {
	        throw new Exception($"Failed to download extension: HTTP {response.StatusCode}");
        }
	}

    private static void UnpackCrxStream(Stream crxStream, string outFolder)
	{
	    crxStream.Position = 0;
	    
	    using var br = new BinaryReader(crxStream, Encoding.UTF8, leaveOpen: true);
	    
	    var signature = new string(br.ReadChars(4));
	    if (signature != "Cr24")
	    {
	        throw new InvalidDataException("Invalid CRX header signature. Expected 'Cr24'.");
	    }
	    
	    var version = br.ReadUInt32();
	    
	    if (version == 2)
	    {
	        // CRX2 format:
	        // - 4 bytes: header size
	        // - header size bytes: public key
	        // - header size bytes: signature
	        var headerSize = br.ReadUInt32();
	        
	        br.ReadBytes((int)headerSize * 2);
	    }
	    else if (version == 3)
	    {
	        // CRX3 format:
	        // - 4 bytes: header size
	        // - 4 bytes: CRX3 header size
	        // - ... more complex header with multiple sections
	        var headerSize = br.ReadUInt32();
	        
	        br.ReadBytes((int)headerSize);
	    }
	    else
	    {
	        throw new InvalidDataException($"Unsupported CRX version: {version}");
	    }
	    
	    var zipDataSize = crxStream.Length - crxStream.Position;
	    var zipData = new byte[zipDataSize];
	    crxStream.ReadExactly(zipData, 0, (int)zipDataSize);
	    
	    using var zipStream = new MemoryStream(zipData);
	    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        
	    if (Directory.Exists(outFolder))
        {
            var filesToDelete = Directory.GetFiles(outFolder)
                .Where(f => !f.EndsWith(".crx", StringComparison.OrdinalIgnoreCase));
            
            foreach (var file in filesToDelete)
            {
                File.Delete(file);
            }
            
            foreach (var dir in Directory.GetDirectories(outFolder))
            {
                Directory.Delete(dir, true);
            }
        }
        else
        {
            Directory.CreateDirectory(outFolder);
        }
        
        archive.ExtractToDirectory(outFolder, overwriteFiles: true);
        
        var manifestPath = Path.Combine(outFolder, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new Exception($"No manifest.json found in extracted extension! It will not be loaded.");
        }
	}
	#endregion
}