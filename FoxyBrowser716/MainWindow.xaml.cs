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

	private record WebsiteTab(CoreWebView2 TabCore, int tabId);
	
	private WebsiteTab[] _tabs;
	
	public MainWindow()
	{
		InitializeComponent();
		// BackImage.Source = Material.Icons.MaterialIconDataProvider.GetData(Material.Icons.MaterialIconKind.Close);
		
		foreach (var button in _normalButtons)
		{
			button.MouseEnter += (_, _) =>
			{
				ChangeColorAnimation(button.Background, MainColor, AccentColor);
			};
			button.MouseLeave += (_, _) =>
			{
				ChangeColorAnimation(button.Background, AccentColor, MainColor);
			};
			button.PreviewMouseLeftButtonDown += (_, _) =>
			{
				button.Foreground = new SolidColorBrush(HighlightColor);
			};
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
		
		ButtonClose.MouseEnter += (_, _) =>
		{
			ChangeColorAnimation(ButtonClose.Background, MainColor, Colors.Red);
		};
		ButtonClose.MouseLeave += (_, _) =>
		{
			ChangeColorAnimation(ButtonClose.Background, Colors.Red, MainColor);
		};
		StateChanged += Window_StateChanged;
		Window_StateChanged(null, EventArgs.Empty);

		Initialize();
	}

	private async void Initialize()
	{
		await WebView.EnsureCoreWebView2Async();

		WebView.SourceChanged += (_, _) =>
		{
			SearchBox.Text = WebView.Source.ToString();
		};
			
		WebView.CoreWebView2.Navigate("https://www.google.com");

	}

	private async void Search_Click(object? s, EventArgs e)
	{
		await WebView.EnsureCoreWebView2Async();

		try
		{
			WebView.CoreWebView2.Navigate(SearchBox.Text);
		}
		catch (Exception exception)
		{
			WebView.CoreWebView2.Navigate($"https://www.google.com/search?q={SearchBox.Text}");
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
				ExitFullscreen();
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