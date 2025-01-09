using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Material.Icons;
using Material.Icons.WPF;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using static FoxyBrowser716.ColorPalette;
using Path = System.IO.Path;

namespace FoxyBrowser716;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	private int _currentTabId;

	private bool _inFullscreen;

	private Rect _originalRectangle;

	private readonly ConcurrentDictionary<int, WebsiteTab> _tabs = [];
	private int? nextToSwap;

	private bool swaping;

	private static readonly ConcurrentDictionary<int, TabInfo> _bookmarks = [];
	private static int _bookmarkCounter;
	
	private static readonly ConcurrentDictionary<int, TabInfo> _pins = [];
	private static int _pinCounter;
	
	public MainWindow()
	{
		InitializeComponent();

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

		AddTabStack.MouseEnter += (_, _) => { ChangeColorAnimation(AddTabStack.Background, MainColor, AccentColor); };
		AddTabStack.MouseLeave += (_, _) => { ChangeColorAnimation(AddTabStack.Background, AccentColor, MainColor); };
		AddTabStack.PreviewMouseLeftButtonDown += (_, _) =>
		{
			AddTabLabel.Foreground = new SolidColorBrush(HighlightColor);
			AddTabButton.Foreground = new SolidColorBrush(HighlightColor);
		};
		AddTabStack.PreviewMouseLeftButtonUp += (_, _) =>
		{
			ChangeColorAnimation(AddTabButton.Foreground, HighlightColor, Colors.White);
			ChangeColorAnimation(AddTabLabel.Foreground, HighlightColor, Colors.White);
			AddTab_Click();
		};
		
		StackPin.MouseEnter += (_, _) => { ChangeColorAnimation(StackPin.Background, MainColor, AccentColor); };
		StackPin.MouseLeave += (_, _) => { ChangeColorAnimation(AddTabStack.Background, AccentColor, MainColor); };
		StackPin.PreviewMouseLeftButtonDown += (_, _) =>
		{
			LabelPin.Foreground = new SolidColorBrush(HighlightColor);
			ButtonPin.Foreground = new SolidColorBrush(HighlightColor);
		};
		StackPin.PreviewMouseLeftButtonUp += async (_, _) =>
		{
			ChangeColorAnimation(ButtonPin.Foreground, HighlightColor, Colors.White);
			ChangeColorAnimation(LabelPin.Foreground, HighlightColor, Colors.White);
			
			var tab = _tabs.TryGetValue(_currentTabId, out var tabRaw) ? tabRaw : new WebsiteTab("https://www.google.com/");

			if (_pins.Any(t => t.Value.Url == tab.TabCore.Source.ToString()))
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
				LabelPin.Content = "Pin Tab";
				await RemovePin(_pins.FirstOrDefault(p => p.Value.Url == tab.TabCore.Source.ToString()).Key);
			}
			else
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.Pin };
				LabelPin.Content = "Unpin Tab";
				await AddPin(tab);
			}
		};
		
		StackBookmark.MouseEnter += (_, _) => { ChangeColorAnimation(StackBookmark.Background, MainColor, AccentColor); };
		StackBookmark.MouseLeave += (_, _) => { ChangeColorAnimation(StackBookmark.Background, AccentColor, MainColor); };
		StackBookmark.PreviewMouseLeftButtonDown += (_, _) =>
		{
			LabelBookmark.Foreground = new SolidColorBrush(HighlightColor);
			ButtonBookmark.Foreground = new SolidColorBrush(HighlightColor);
		};
		StackBookmark.PreviewMouseLeftButtonUp += (_, _) =>
		{
			ChangeColorAnimation(ButtonBookmark.Foreground, HighlightColor, Colors.White);
			ChangeColorAnimation(LabelBookmark.Foreground, HighlightColor, Colors.White);
			MessageBox.Show("Not implemented yet");
		};

		LeftBar.MouseEnter += (_, _) =>
		{
			var animation = new DoubleAnimation
			{
				Duration = TimeSpan.FromSeconds(0.25),
				To = 230,
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};

			// Apply the animation to the Grid's Height
			LeftBar.BeginAnimation(WidthProperty, animation);
		};
		LeftBar.MouseLeave += (_, _) =>
		{
			var animation = new DoubleAnimation
			{
				Duration = TimeSpan.FromSeconds(0.25),
				To = 30,
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};

			// Apply the animation to the Grid's Height
			LeftBar.BeginAnimation(WidthProperty, animation);
		};

		SearchBox.KeyDown += (_, e) =>
		{
			if (e.Key == Key.Enter)
			{
				Search_Click(null, EventArgs.Empty);
			}
		};
	}

	private Control[] _normalButtons =>
	[
		ButtonMaximize,
		ButtonMinimize,
		ButtonMenu,
		SearchButton,
		RefreshButton,
		BackButton, //TODO: need custom animation logic.
		ForwardButton
	];

	private async void AddTab_Click()
	{
		var id = AddTab("https://www.google.com/");
		await SwapActiveTab(id);
	}
	
	private void RefreshPins()
	{
		if (_tabs.TryGetValue(_currentTabId, out var tab))
		{
			if (_pins.Any(t => t.Value.Url == tab.TabCore.Source.ToString()))
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.Pin };
				LabelPin.Content = "Unpin Tab";
			}
			else
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
				LabelPin.Content = "Pin Tab";
			}
		}
		PinnedTabs.Children.Clear();
		foreach (var pin in _pins)
		{
			var stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Height = 30,
				Background = new SolidColorBrush(Colors.Transparent)
			};

			var button = new Button
			{
				Content = pin.Value.Image,
				Width = 30,
				Height = 30,
				Background = new SolidColorBrush(Colors.Transparent),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			var titleBox = new Label
			{
				Height = 30,
				Content = pin.Value.Title,
				Foreground = Brushes.White,
				Width = 170,
				Background = new SolidColorBrush(Colors.Transparent),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			var closeButton = new Button
			{
				Content = new MaterialIcon { Kind = MaterialIconKind.Close },
				Width = 30,
				Height = 30,
				Background = new SolidColorBrush(Colors.Transparent),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				Foreground = new SolidColorBrush(Colors.White),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			closeButton.PreviewMouseLeftButtonUp += async (_, _) => await RemovePin(pin.Key);

			closeButton.MouseEnter += (_, _) =>
			{
				ChangeColorAnimation(closeButton.Foreground, Colors.White, Colors.Red);
			};
			closeButton.MouseLeave += (_, _) =>
			{
				ChangeColorAnimation(closeButton.Foreground, Colors.Red, Colors.White);
			};

			stackPanel.MouseEnter += (_, _) =>
			{
				ChangeColorAnimation(stackPanel.Background, MainColor, AccentColor);
			};
			stackPanel.MouseLeave += (_, _) =>
			{
				ChangeColorAnimation(stackPanel.Background, AccentColor, MainColor);
			};

			button.PreviewMouseLeftButtonDown += async (_, _) => { await SwapActiveTab(AddTab(pin.Value.Url)); };
			titleBox.PreviewMouseLeftButtonDown += async (_, _) => { await SwapActiveTab(AddTab(pin.Value.Url)); };
			
			stackPanel.Children.Add(button);
			stackPanel.Children.Add(titleBox);
			stackPanel.Children.Add(closeButton);

			PinnedTabs.Children.Add(stackPanel);
		}
	}
	
	private async Task RemovePin(int id)
	{
		_pins.TryRemove(id, out _);
		
		var saveTask = TabInfo.SaveTabs(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pins.json"), _pins.Values.ToArray());
		
		RefreshPins();
		
		await saveTask;
	}
	
	private async Task AddPin(WebsiteTab tab)
	{
		var newInfo = new TabInfo()
		{
			Title = tab.Title,
			Url = tab.TabCore.Source.ToString(),
			Image = tab.Icon
		};
		
		_pins.TryAdd(_pinCounter++, newInfo);
		
		var saveTask = TabInfo.SaveTabs(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pins.json"), _pins.Values.ToArray());
		
		RefreshPins();
		
		await saveTask;
	}

	private void RefreshTabs()
	{
		Tabs.Children.Clear();
		Tabs.Children.Add(AddTabStack);
		foreach (var tab in _tabs.Values)
		{
			var stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Height = 30,
				Background = new SolidColorBrush(Colors.Transparent)
			};

			var button = new Button
			{
				Content = tab.Icon,
				Width = 30,
				Height = 30,
				Background = new SolidColorBrush(Colors.Transparent),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			var titleBox = new Label
			{
				Height = 30,
				Content = tab.Title,
				Foreground = Brushes.White,
				Width = 170,
				Background = new SolidColorBrush(Colors.Transparent),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			var closeButton = new Button
			{
				Content = new MaterialIcon { Kind = MaterialIconKind.Close },
				Width = 30,
				Height = 30,
				Background = new SolidColorBrush(Colors.Transparent),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				Foreground = new SolidColorBrush(Colors.White),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			

			tab.UrlChanged += () =>
			{
				if (tab.TabId == _currentTabId)
				{
					SearchBox.Text = tab.TabCore.Source.ToString();
					BackButton.Foreground = tab.TabCore.CanGoBack
						? new SolidColorBrush(Color.FromRgb(255, 255, 255))
						: new SolidColorBrush(Color.FromRgb(100, 100, 100));
					ForwardButton.Foreground = tab.TabCore.CanGoForward
						? new SolidColorBrush(Color.FromRgb(255, 255, 255))
						: new SolidColorBrush(Color.FromRgb(100, 100, 100));
				}

				if (_pins.Any(t => t.Value.Url == tab.TabCore.Source.ToString()))
				{
					ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.Pin };
					LabelPin.Content = "Unpin Tab";
				}
				else
				{
					ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
					LabelPin.Content = "Pin Tab";
				}
			};

			tab.TitleChanged += () =>
			{
				titleBox.Content = tab.Title;
			};

			tab.ImageChanged += () => { button.Content = tab.Icon; };

			closeButton.PreviewMouseLeftButtonUp += (_, _) => { RemoveTab(tab.TabId); };

			closeButton.MouseEnter += (_, _) =>
			{
				ChangeColorAnimation(closeButton.Foreground, Colors.White, Colors.Red);
			};
			closeButton.MouseLeave += (_, _) =>
			{
				ChangeColorAnimation(closeButton.Foreground, Colors.Red, Colors.White);
			};

			stackPanel.MouseEnter += (_, _) =>
			{
				if (tab.TabId == _currentTabId) return;
				ChangeColorAnimation(stackPanel.Background, MainColor, AccentColor);
			};
			stackPanel.MouseLeave += (_, _) =>
			{
				if (tab.TabId == _currentTabId) return;
				ChangeColorAnimation(stackPanel.Background, AccentColor, MainColor);
			};

			button.PreviewMouseLeftButtonDown += async (_, _) => { await SwapActiveTab(tab.TabId); };
			titleBox.PreviewMouseLeftButtonDown += async (_, _) => { await SwapActiveTab(tab.TabId); };

			if (tab.TabId == _currentTabId) stackPanel.Background = new SolidColorBrush(HighlightColor);

			stackPanel.Children.Add(button);
			stackPanel.Children.Add(titleBox);
			stackPanel.Children.Add(closeButton);

			Tabs.Children.Add(stackPanel);
		}
	}
	
	private ContextMenu _menu;

	private void ButtonMenu_Click(object sender, RoutedEventArgs e)
	{
		// Ensure old menu is cleared
		if (_menu != null && _menu.IsOpen)
		{
			_menu.IsOpen = false;
			return;
		}

		// Create the context menu
		_menu = new ContextMenu
		{
			Background = new SolidColorBrush(Color.FromRgb(30, 30, 45)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(255, 145, 15)),
			BorderThickness = new Thickness(1),
			Padding = new Thickness(0),    
			Margin = new Thickness(0),
			StaysOpen = true,
			Focusable = true,
			HorizontalOffset = 30,
		};

		// Add menu items
		var menuItems = new Dictionary<string, Action>
		{
			{ "⚙️ Settings", OpenSettings },
			{ "📑 Bookmarks", OpenBookmarks },
			{ "🕒 History", OpenHistory },
			{ "⬇️ Downloads", OpenDownloads },
			{ "🛠️ Extensions", OpenExtensions }
		};

		foreach (var menuItem in menuItems)
		{
			var item = new MenuItem
			{
				Header = menuItem.Key,
				Background = Brushes.Transparent,
				Foreground = Brushes.White,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Padding = new Thickness(5),  // Nice padding for items
				FontSize = 14,
				Focusable = false, // Disable annoying highlight outlines
			};

			item.MouseEnter += (s, _) =>
				((MenuItem)s).Background = new SolidColorBrush(Color.FromRgb(40, 40, 60));
			item.MouseLeave += (s, _) =>
				((MenuItem)s).Background = Brushes.Transparent;

			item.Click += (_, _) => menuItem.Value?.Invoke();
			_menu.Items.Add(item);
		}

		// Open it below the button
		_menu.PlacementTarget = (Button)sender;
		_menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
		_menu.IsOpen = true;

		// Handle clean-up
		_menu.Closed += (_, _) => _menu = null; // Reset menu on close
	}

// Menu action handlers (replace with your actual logic)
private void OpenSettings() => MessageBox.Show("Settings clicked!");
private void OpenBookmarks() => MessageBox.Show("Bookmarks clicked!");
private void OpenHistory() => MessageBox.Show("History clicked!");
private void OpenDownloads() => MessageBox.Show("Downloads clicked!");
private void OpenExtensions() => MessageBox.Show("Extensions clicked!");
	private async void Initialize()
	{
		var loadPinTask = TabInfo.TryLoadTabs(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pins.json"));
		
		var options = new CoreWebView2EnvironmentOptions();
		options.AreBrowserExtensionsEnabled = true;
		options.AllowSingleSignOnUsingOSPrimaryAccount = true;
		WebsiteEnvironment ??= await CoreWebView2Environment.CreateAsync(null, "UserData", options);
		var id = AddTab("https://www.google.com/");

		await SwapActiveTab(id);
		
		var pins = await loadPinTask;
		foreach (var pin in pins)
		{
			_pins.TryAdd(_pinCounter++, pin);
		}
		RefreshPins();
	}

	/// <summary>
	/// This event fires whenever the amount of tabs are changed via the "AddTab," "RemoveTab," and "SwapTab" functions.
	/// </summary>
	private event Action tabsChanged;

	/// <summary>
	///  Creates a new tab and adds it to the _tabs dictionary.
	/// </summary>
	/// <param name="url">the requested starting url</param>
	/// <returns>The id of the tab. This id can be used with the dictionary _tabs as the key.</returns>
	private int AddTab(string url)
	{
		var tab = new WebsiteTab(url);
		_tabs.TryAdd(tab.TabId, tab);
		TabHolder.Children.Add(tab.TabCore);
		tabsChanged?.Invoke();
		return tab.TabId;
	}

	private async void RemoveTab(int id)
	{
		//check if the current tab is in use
		if (_currentTabId == id)
		{
			if (_tabs.Count > 1)
			{
				var newId = _tabs.First(t => t.Key != id).Key;
				await SwapActiveTab(newId);
			}
			else
			{
				var webView = _tabs.First().Value.TabCore;
				webView.Source = new Uri("https://www.google.com/");
				return;
			}
		}

		//remove
		if (_tabs.TryRemove(id, out var tabToRemove))
		{
			tabToRemove.TabCore.Dispose();
			tabsChanged?.Invoke();
		}
	}

	/// <summary>
	/// Swaps to another tab.
	/// </summary>
	/// <param name="id">The id of the tab to swap to.</param>
	private async Task SwapActiveTab(int id)
	{
		if (swaping)
		{
			nextToSwap = id;
			return;
		}

		swaping = true;

		if (_tabs.TryGetValue(id, out var tab))
		{
			_currentTabId = id;
			await tab.SetupTask;
			var tabcore = tab.TabCore;
		
			if (tabcore.CoreWebView2.IsSuspended) tabcore.CoreWebView2.Resume();

			foreach (var t in _tabs.Where(t => t.Value.TabId != id && !tabcore.CoreWebView2.IsSuspended))
			{
				await t.Value.TabCore.EnsureCoreWebView2Async(WebsiteEnvironment);
				if (t.Value.TabCore.CoreWebView2.IsDocumentPlayingAudio)
					continue;
				t.Value.TabCore.CoreWebView2.TrySuspendAsync();
			}

			foreach (var t in _tabs)
				t.Value.TabCore.Visibility = t.Value.TabId == _currentTabId
					? Visibility.Visible
					: Visibility.Collapsed;
		
			SearchBox.Text = tabcore.CoreWebView2.Source;
			BackButton.Foreground = tabcore.CanGoBack
				? new SolidColorBrush(Color.FromRgb(255, 255, 255))
				: new SolidColorBrush(Color.FromRgb(100, 100, 100));
			ForwardButton.Foreground = tabcore.CanGoForward
				? new SolidColorBrush(Color.FromRgb(255, 255, 255))
				: new SolidColorBrush(Color.FromRgb(100, 100, 100));
			tabsChanged?.Invoke();
		}

		swaping = false;
		if (nextToSwap is { } next)
		{
			nextToSwap = null;
			await SwapActiveTab(next);
		}
	}

	private async void Search_Click(object? s, EventArgs e)
	{
		if (_tabs.TryGetValue(_currentTabId, out var tab))
		{
			var tabCore = tab.TabCore;
			await tabCore.EnsureCoreWebView2Async(WebsiteEnvironment);

			try
			{
				tabCore.CoreWebView2.Navigate(SearchBox.Text);
			}
			catch (Exception exception)
			{
				tabCore.CoreWebView2.Navigate($"https://www.google.com/search?q={SearchBox.Text}");
			}
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

	/// <summary>
	/// Changes the color of a brush to do cool animations!
	/// </summary>
	/// <param name="brush">The brush that is animated such as "Control.Background." NOTE: The brush has to be custom (i.e. using new brush or specifying the color as hex '#000000' in xml)</param>
	/// <param name="from">The color that the animation start from</param>
	/// <param name="to">The color that the animation ends on (final color)</param>
	/// <param name="time"></param>
	private static void ChangeColorAnimation(Brush brush, Color from, Color to, double time = 0.2)
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

	private void MaximizeRestore_Click(object? sender, EventArgs e)
	{
		if (_inFullscreen)
			ExitFullscreen();
		else
			EnterFullscreen();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void RefreshButton_OnClick_Click(object sender, RoutedEventArgs e)
	{
		if (_tabs.TryGetValue(_currentTabId, out var tab)) tab.TabCore.Reload();
	}

	private async void BackButton_OnClick(object sender, RoutedEventArgs e)
	{
		if (_tabs.TryGetValue(_currentTabId, out var tab)) tab.TabCore.GoBack();
	}

	private async void ForwardButton_OnClick(object sender, RoutedEventArgs e)
	{
		if (_tabs.TryGetValue(_currentTabId, out var tab)) tab.TabCore.GoForward();
	}

	#region FullscreenStuff

	private void EnterFullscreen()
	{
		try
		{
			_inFullscreen = true;
			_originalRectangle = new Rect(Left, Top, Width, Height);

			var screen = GetCurrentScreen();

			Left = screen.Left;
			Top = screen.Top;
			Width = screen.Width;
			Height = screen.Height;
			ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.FullscreenExit };
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, $"Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void ExitFullscreen()
	{
		try
		{
			_inFullscreen = false;
			Left = _originalRectangle.Left;
			Top = _originalRectangle.Top;
			Width = _originalRectangle.Width;
			Height = _originalRectangle.Height;
			ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.Fullscreen };
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, $"Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private Rect GetCurrentScreen()
	{
		try
		{
			var hWnd = new WindowInteropHelper(this).Handle;

			// Get the monitor associated with the window handle
			var hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);

			// Initialize the MONITORINFO structure
			var monitorInfo = new MONITORINFO();
			monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

			// Get the monitor information
			if (GetMonitorInfo(hMonitor, ref monitorInfo))
				return new Rect(monitorInfo.rcMonitor.Left, monitorInfo.rcMonitor.Top, monitorInfo.rcMonitor.Width,
					monitorInfo.rcMonitor.Height);

			// Return default screen if monitor info fails
			return SystemParameters.WorkArea with { X = 0, Y = 0 };
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, $"Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
			return new Rect(0, 0, 200, 100);
		}
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
	private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

	#endregion

	#endregion

	#region ResizeWindows

	private bool ResizeInProcess;
	public static CoreWebView2Environment? WebsiteEnvironment { get; private set; }

	private void Resize_Init(object sender, MouseButtonEventArgs e)
	{
		try
		{
			var senderRect = sender as Rectangle;
			if (senderRect != null && !_inFullscreen)//TODO: verify that this works
			{
				ResizeInProcess = true;
				senderRect.CaptureMouse();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, $"Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void Resize_End(object sender, MouseButtonEventArgs e)
	{
		try
		{
			var senderRect = sender as Rectangle;
			if (senderRect != null)
			{
				ResizeInProcess = false;
				senderRect.ReleaseMouseCapture();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, $"Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void Resizeing_Form(object sender, MouseEventArgs e)
	{
		try
		{
			if (ResizeInProcess)
			{
				var senderRect = sender as Rectangle;
				var mainWindow = senderRect.Tag as Window;
				if (senderRect != null)
				{
					var width = e.GetPosition(mainWindow).X;
					var height = e.GetPosition(mainWindow).Y;
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
						if (width > 0) mainWindow.Width = width;
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
						if (height > 0) mainWindow.Height = height;
					}
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, $"Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	#endregion
}