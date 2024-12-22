using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Material.Icons;
using Material.Icons.WPF;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using static FoxyBrowser716.ColorPalette;

namespace FoxyBrowser716;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	private Control[] _normalButtons =>
	[
		ButtonMaximize,
		ButtonMinimize,
		ButtonMenu,
		ButtonSidePanel,
		SearchButton,
		TabHome,
		ButtonPin,
		ButtonBookmark
	];

	private int _currentTabId;
	private int _tabCounter;

	private Rect _originalRectangle;

	private record WebsiteTab
	{
		public event Action UrlChanged;
		public event Action ImageChanged;
		
		public WebView2 TabCore;
		public int TabId;
		public Image Icon;
		public string Title;
		public WebsiteTab(WebView2 tabCore,  int tabId)
		{
			TabCore = tabCore;
			TabId = tabId;

			Initialize();
		}

		private async Task Initialize()
		{
			await TabCore.EnsureCoreWebView2Async();
			
			Title = TabCore.CoreWebView2.DocumentTitle;
			
			Icon = new Image
			{
				Source = CreateCircleWithLetter(26,26, Title.Length > 0 ? Title[0].ToString() : "_", Brushes.DimGray, Brushes.White),
				Width = 26,
				Height = 26,
				Margin = new Thickness(1),
			}; 
			
			RefreshImage();
			
			TabCore.SourceChanged += (_,_) =>
			{
				Title = TabCore.CoreWebView2.DocumentTitle;
				UrlChanged?.Invoke();
				RefreshImage();
			};
		}
		
		private async Task RefreshImage()
		{
			try
			{
				Icon = new Image
				{
					Source = await GetImageSourceFromStreamAsync(await TabCore.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png)),
					Width = 26,
					Height = 26,
					Margin = new Thickness(1),
				};
			}
			catch
			{
				Icon = new Image
				{
					Source = CreateCircleWithLetter(26,26,Title.Length > 0 ? Title[0].ToString() : "", Brushes.DimGray, Brushes.White),
					Width = 26,
					Height = 26,
					Margin = new Thickness(1),
				}; 
			}
			ImageChanged?.Invoke();
		}
	};
	
	private static async Task<ImageSource> GetImageSourceFromStreamAsync(Stream stream)
	{
		if (stream == null)
			return null;

		var bitmap = new BitmapImage();
		bitmap.BeginInit();
		bitmap.StreamSource = stream;
		bitmap.CacheOption = BitmapCacheOption.OnLoad;
		bitmap.EndInit();

		stream.Close();

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
				System.Globalization.CultureInfo.CurrentCulture,
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

	private List<WebsiteTab> _tabs = [];

	public MainWindow()
	{
		InitializeComponent();
		// BackImage.Source = Material.Icons.MaterialIconDataProvider.GetData(Material.Icons.MaterialIconKind.Close);

		foreach (var button in _normalButtons)
		{
			button.MouseEnter += (_, _) => { ChangeColorAnimation(button.Background, MainColor, AccentColor); };
			button.MouseLeave += (_, _) => { ChangeColorAnimation(button.Background, AccentColor, MainColor); };
			button.PreviewMouseLeftButtonDown += (_, _) => { button.Foreground = new SolidColorBrush(HighlightColor); };
			button.PreviewMouseLeftButtonUp += (_, _) =>
			{
				ChangeColorAnimation(button.Foreground, HighlightColor, Colors.White);
			};
		}

		SearchBox.GotKeyboardFocus += (_, _) =>
		{
			ChangeColorAnimation(SearchBackground.BorderBrush, Colors.White, HighlightColor);
		};
		SearchBox.LostKeyboardFocus += (_, _) =>
		{
			ChangeColorAnimation(SearchBackground.BorderBrush, HighlightColor, Colors.White);
		};

		ButtonClose.MouseEnter += (_, _) => { ChangeColorAnimation(ButtonClose.Background, MainColor, Colors.Red); };
		ButtonClose.MouseLeave += (_, _) => { ChangeColorAnimation(ButtonClose.Background, Colors.Red, MainColor); };
		StateChanged += Window_StateChanged;
		Window_StateChanged(null, EventArgs.Empty);
		
		tabsChanged += RefreshTabs;

		Initialize();
		ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.Fullscreen };

	}

	private void RefreshTabs()
	{
		Tabs.Children.Clear();
		Tabs.Children.Add(TabHome);
		foreach (var tab in _tabs)
		{
			var button = new Button
			{
				Content = tab.Icon,
				Width = 30,
				Height = 30,
				Background = new SolidColorBrush(Colors.Transparent),
			};

			tab.UrlChanged += () =>
			{
				if (tab.TabId == _currentTabId)
				{
					SearchBox.Text = tab.TabCore.Source.ToString();
				}

				///tab.Title;
			};

			tab.ImageChanged += () =>
			{
				button.Content = tab.Icon;
			};
				
			button.VerticalContentAlignment = VerticalAlignment.Stretch;
			button.HorizontalContentAlignment = HorizontalAlignment.Stretch;

			button.MouseEnter += (_, _) => { ChangeColorAnimation(button.Background, MainColor, AccentColor); }; 
			button.MouseLeave += (_, _) => { ChangeColorAnimation(button.Background, AccentColor, MainColor); };

			button.PreviewMouseLeftButtonDown += async (_, _) =>
			{
				await SwapActiveTab(tab.TabId);
			};

			Tabs.Children.Add(button);
		}
	}

	private async void Initialize()
	{
		var webView = await CreateTab("https://www.google.com/");
		AddTab(webView);
		
		AddTab(await CreateTab("https://music.youtube.com/watch?v=5Duje_sZko8"));
		AddTab(await CreateTab("https://music.youtube.com/watch?v=_egkqeKLUMY&si=CA9YlwUmG3kts2Si"));
		AddTab(await CreateTab("https://discord.com/"));
		AddTab(await CreateTab("https://store.steampowered.com/"));


		await SwapActiveTab(0);
	}

	private async Task<WebView2> CreateTab(string url)
	{
		var webView = new WebView2();
		webView.EnsureCoreWebView2Async();
		
		try
		{
			webView.Source = new Uri(url);
		}
		catch (Exception exception)
		{
			webView.Source = new Uri($"https://www.google.com/search?q={url}");
		}
		
		return webView;
	}
	
	private event Action tabsChanged;
	private void AddTab(WebView2 tabCore)
	{
		_tabs.Add(new WebsiteTab(tabCore, _tabCounter++));
		tabsChanged?.Invoke();
	}
	private void RemoveTab(int id)
	{
		if (_tabs.FirstOrDefault(t => t.TabId == id) is {} tabToRemove)
		{
			_tabs.Remove(tabToRemove);
			tabsChanged?.Invoke();
		}
	}
	private async Task SwapActiveTab(int id)
	{
		var tabcore = _tabs.First(t => t.TabId == id).TabCore;
		_currentTabId = id;
		MainPanel.Child = tabcore;
		await tabcore.EnsureCoreWebView2Async();
		SearchBox.Text = tabcore.CoreWebView2.Source;
		tabsChanged?.Invoke();
	}
	private async Task SwapTabLocation(int id)
	{
		tabsChanged?.Invoke();
	}

	private async void Search_Click(object? s, EventArgs e)
	{
		var tabCore = _tabs.First(t => t.TabId == _currentTabId).TabCore;
		await tabCore.EnsureCoreWebView2Async();

		try
		{
			tabCore.CoreWebView2.Navigate(SearchBox.Text);
		}
		catch (Exception exception)
		{
			tabCore.CoreWebView2.Navigate($"https://www.google.com/search?q={SearchBox.Text}");
		}
	}
	
	private void Window_StateChanged(object? sender, EventArgs e)
	{
		if (WindowState == WindowState.Maximized)
		{
			WindowState = WindowState.Normal;
			EnterFullscreen();
		}
	}
	
	private static void ChangeColorAnimation(Brush brush, Color from, Color to, double time=0.2)
	{
		var colorAnimation = new ColorAnimation
		{
			From = from,
			To = to,
			Duration = new Duration(TimeSpan.FromSeconds(time)),
			EasingFunction = new QuadraticEase()
		};
		brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
	}
	
	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount == 2)
		{
			MaximizeRestore_Click(null, EventArgs.Empty);
		}
		else
		{
			if (_inFullscreen)
				return;
			
			_originalRectangle = new Rect(Left, Top, Width, Height);
			DragMove();
		}
	}

	private void Minimize_Click(object? sender, EventArgs e)
	{
		WindowState = WindowState.Minimized;
	}

	private bool _inFullscreen;
	private void MaximizeRestore_Click(object? sender, EventArgs e)
	{
		if (_inFullscreen)
			ExitFullscreen();
		else
			EnterFullscreen();
	}

	#region FullscreenStuff
	private void EnterFullscreen()
	{
		_inFullscreen = true;
		_originalRectangle = new Rect(Left, Top, Width, Height);
		
		var screen = GetCurrentScreen();
		
		this.Left = screen.Left;
		this.Top = screen.Top;
		this.Width = screen.Width;
		this.Height = screen.Height;
		ButtonMaximize.Content =new MaterialIcon { Kind = MaterialIconKind.FullscreenExit };
	}

	private void ExitFullscreen()
	{
		_inFullscreen = false;
		this.Left = _originalRectangle.Left;
		this.Top = _originalRectangle.Top;
		this.Width = _originalRectangle.Width;
		this.Height = _originalRectangle.Height;
		ButtonMaximize.Content =new MaterialIcon { Kind = MaterialIconKind.Fullscreen };
	}
	
	private Rect GetCurrentScreen()
	{
		var hWnd = new WindowInteropHelper(this).Handle;

		// Get the monitor associated with the window handle
		IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);

		// Initialize the MONITORINFO structure
		MONITORINFO monitorInfo = new MONITORINFO();
		monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

		// Get the monitor information
		if (GetMonitorInfo(hMonitor, ref monitorInfo))
		{
			return new Rect(monitorInfo.rcMonitor.Left, monitorInfo.rcMonitor.Top, monitorInfo.rcMonitor.Width, monitorInfo.rcMonitor.Height);
		}

		// Return default screen if monitor info fails
		return new Rect(0, 0, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
	}

	#region Windows API Methods

	// Get the monitor's handle from a window handle
	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

	// Get information about a monitor
	[DllImport("user32.dll")]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

	// Monitor Info Struct
	[StructLayout(LayoutKind.Sequential)]
	public struct MONITORINFO
	{
		public int cbSize;
		public RECT rcMonitor;
		public RECT rcWork;
		public uint dwFlags;
	}

	// Rect struct to define monitor size
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public int Width => Right - Left;
		public int Height => Bottom - Top;
	}

	// Constants
	const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

	#endregion
	#endregion

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
	
	#region ResizeWindows
	bool ResizeInProcess = false;
	private void Resize_Init(object sender, MouseButtonEventArgs e)
	{
		Rectangle senderRect = sender as Rectangle;
		if (senderRect != null)
		{
			ResizeInProcess = true;
			senderRect.CaptureMouse();
		}
	}

	private void Resize_End(object sender, MouseButtonEventArgs e)
	{
		Rectangle senderRect = sender as Rectangle;
		if (senderRect != null)
		{
			ResizeInProcess = false; ;
			senderRect.ReleaseMouseCapture();
		}
	}

	private void Resizeing_Form(object sender, MouseEventArgs e)
	{
		if (ResizeInProcess)
		{
			Rectangle senderRect = sender as Rectangle;
			Window mainWindow = senderRect.Tag as Window;
			if (senderRect != null)
			{
				double width = e.GetPosition(mainWindow).X;
				double height = e.GetPosition(mainWindow).Y;
				senderRect.CaptureMouse();
				if (senderRect.Name.ToLower().Contains("right"))
				{
					width += 1;
					if (width > 0)
						mainWindow.Width = width;
				}
				if (senderRect.Name.ToLower().Contains("left"))
				{
					width -= 1;
					mainWindow.Left += width;
					width = mainWindow.Width - width;
					if (width > 0)
					{
						mainWindow.Width = width;
					}
				}
				if (senderRect.Name.ToLower().Contains("bottom"))
				{
					height += 1;
					if (height > 0)
						mainWindow.Height = height;
				}
				if (senderRect.Name.ToLower().Contains("top"))
				{
					height -= 1;
					mainWindow.Top += height;
					height = mainWindow.Height - height;
					if (height > 0)
					{
						mainWindow.Height = height;
					}
				}
			}
		}
	}
	#endregion
}