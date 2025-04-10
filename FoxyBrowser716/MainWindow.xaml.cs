
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Material.Icons;
using Material.Icons.WPF;
using static FoxyBrowser716.ColorPalette;
using static FoxyBrowser716.Animator;


namespace FoxyBrowser716;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private HomePage _homePage;

	private bool _inFullscreen;

	private Rect _originalRectangle;
	
	internal readonly TabManager TabManager;

	internal readonly ServerManager OwnerInstance;

	internal Task _initTask;
	
	public MainWindow(ServerManager owner)
	{
		OwnerInstance = owner;
		
		InitializeComponent();

		TabManager = new TabManager();
		_initTask = Initialize();
		
		//TODO: find a better way to do all this crazy GUI stuff
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
		
		ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.Fullscreen };

		AddTabStack.MouseEnter += (_, _) => { if(TabManager.ActiveTabId != -1) ChangeColorAnimation(AddTabStack.Background, MainColor, AccentColor); };
		AddTabStack.MouseLeave += (_, _) => { if(TabManager.ActiveTabId != -1) ChangeColorAnimation(AddTabStack.Background, AccentColor, MainColor); };
		AddTabStack.PreviewMouseLeftButtonDown += (_, _) =>
		{
			AddTabLabel.Foreground = new SolidColorBrush(HighlightColor);
			AddTabButton.Foreground = new SolidColorBrush(HighlightColor);
		};
		AddTabStack.PreviewMouseLeftButtonUp += (_, _) =>
		{
			ChangeColorAnimation(AddTabButton.Foreground, HighlightColor, Colors.White);
			ChangeColorAnimation(AddTabLabel.Foreground, HighlightColor, Colors.White);
			TabManager.SwapActiveTabTo(-1);
		};
		
		StackPin.MouseEnter += (_, _) => { ChangeColorAnimation(StackPin.Background, MainColor, AccentColor); };
		StackPin.MouseLeave += (_, _) => { ChangeColorAnimation(StackPin.Background, AccentColor, MainColor); };
		StackPin.PreviewMouseLeftButtonDown += (_, _) =>
		{
			LabelPin.Foreground = new SolidColorBrush(HighlightColor);
			ButtonPin.Foreground = new SolidColorBrush(HighlightColor);
		};
		StackPin.PreviewMouseLeftButtonUp += (_, _) =>
		{
			ChangeColorAnimation(ButtonPin.Foreground, HighlightColor, Colors.White);
			ChangeColorAnimation(LabelPin.Foreground, HighlightColor, Colors.White);
			
			var tab = TabManager.GetTab(TabManager.ActiveTabId);
			
			if (tab is null) return;

			if (OwnerInstance.BrowserData.PinInfo.GetAllTabInfos().Any(t => t.Value.Url == (tab?.TabCore?.Source?.ToString()??"__NULL__")))
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
				LabelPin.Content = "Pin Tab";
				OwnerInstance.BrowserData.PinInfo.RemoveTabInfo(OwnerInstance.BrowserData.PinInfo.GetAllTabInfos()
					.FirstOrDefault(p => p.Value.Url == tab.TabCore.Source.ToString()).Key);
			}
			else
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.Pin };
				LabelPin.Content = "Unpin Tab";
				OwnerInstance.BrowserData.PinInfo.AddTabInfo(tab);
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

		LeftBar.MouseEnter += (_, _) => { OpenSideBar(); };
		LeftBar.MouseLeave += (_, _) => { CloseSideBar(); };

		SearchBox.KeyDown += (_, e) =>
		{
			if (e.Key == Key.Enter)
			{
				Search_Click(null, EventArgs.Empty);
			}
		};
	}

	internal bool SideOpen;
	internal void OpenSideBar()
	{
		SideOpen = true;
		var animation = new DoubleAnimation
		{
			Duration = TimeSpan.FromSeconds(0.25),
			To = 260,
			EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			}
		};

		LeftBar.BeginAnimation(WidthProperty, animation);
	}
	
	internal void CloseSideBar()
	{
		SideOpen = false;
		var animation = new DoubleAnimation
		{
			Duration = TimeSpan.FromSeconds(0.25),
			To = 30,
			EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			}
		};

		LeftBar.BeginAnimation(WidthProperty, animation);
	}

	internal Rect GetLeftBarDropArea()
	{
		var p = PointToScreen(new Point(0, 30));
		return new Rect(p.X, p.Y, 260, LeftBar.ActualHeight);
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
	
	private ContextMenu _menu;

	private void ButtonMenu_Click(object sender, RoutedEventArgs e)
	{
		if (_menu is { IsOpen: true })
		{
			_menu.IsOpen = false;
			return;
		}

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

		var menuItems = new Dictionary<string, Action>
		{
			{ "Settings", OpenSettings },
			{ "Bookmarks", OpenBookmarks },
			{ "History", OpenHistory },
			{ "Downloads", OpenDownloads },
			{ "Extensions", OpenExtensions }
		};

		foreach (var menuItem in menuItems)
		{
			var item = new MenuItem
			{
				Header = menuItem.Key,
				Background = Brushes.Transparent,
				Foreground = Brushes.White,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Padding = new Thickness(5),
				FontSize = 14,
				Focusable = false,
			};

			item.MouseEnter += (s, _) =>
				((MenuItem)s).Background = new SolidColorBrush(Color.FromRgb(40, 40, 60));
			item.MouseLeave += (s, _) =>
				((MenuItem)s).Background = Brushes.Transparent;

			item.Click += (_, _) => menuItem.Value?.Invoke();
			_menu.Items.Add(item);
		}

		_menu.PlacementTarget = (Button)sender;
		_menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
		_menu.IsOpen = true;

		_menu.Closed += (_, _) => _menu = null;
	}

// Menu action handlers (replace with your actual logic)
private void OpenSettings()
{
}

private void OpenBookmarks()
{
}

private void OpenHistory()
{
}

private void OpenDownloads()
{
}

private void OpenExtensions()
{
}

private async void OnTabCardDragChanged(TabCard sender, int? relativeMove)
{
	try
	{
		if (relativeMove == null)
		{
			//TODO: move to tab window
			await BuildNewWindow(sender);
		}
		else
		{
			await UpdateTabOrder(sender, relativeMove.Value);
		}
	}
	catch (Exception e)
	{
		// TODO handle exception
	}
}

private async Task UpdateTabOrder(TabCard tabCard, int moveTo)
{
	if (moveTo < 1 || moveTo >= Tabs.Children.Count)
	{
		if (moveTo > Tabs.Children.Count + 5)
			await BuildNewWindow(tabCard);
		return;
	}

	Tabs.Children.Remove(tabCard);
	Tabs.Children.Insert(moveTo, tabCard);
}

private bool _buildingWindow;
private int _createdFor = -1;
private async Task BuildNewWindow(TabCard tabCard)
{
	if (_buildingWindow || _createdFor == tabCard.Key) return;
	_buildingWindow = true;

	if (TabManager.GetTab(tabCard.Key) is not { } tab) return;
	
	var point = PointToScreen(Mouse.GetPosition(null));
	var tabCardWindow = new TabMoveWindowCard(OwnerInstance, tab)
	{
		Top = point.Y - 15,
		Left = point.X - 75
	};
	tabCardWindow.Show();
	TabManager.RemoveTab(tabCard.Key, true);
	
	_createdFor = tabCard.Key;
	_buildingWindow = false;
}

private async Task Initialize()
{
	StackBookmark.Visibility = Visibility.Collapsed;
	StackPin.Visibility = Visibility.Collapsed;
	NavigationGrid.Visibility = Visibility.Collapsed;
	
	TabManager.ActiveTabChanged += (oldActiveTab, newActiveTab) =>
	{
		StackBookmark.Visibility = newActiveTab == -1 ? Visibility.Collapsed : Visibility.Visible;
		StackPin.Visibility = newActiveTab == -1 ? Visibility.Collapsed : Visibility.Visible;
		NavigationGrid.Visibility = newActiveTab == -1 ? Visibility.Collapsed : Visibility.Visible;
		_homePage.Visibility = newActiveTab == -1 ? Visibility.Visible : Visibility.Collapsed;
		
		AddTabStack.Background = newActiveTab == -1
			? new SolidColorBrush(HighlightColor)
			: new SolidColorBrush(MainColor);
		_homePage.Visibility = newActiveTab == -1
			? Visibility.Visible
			: Visibility.Collapsed;
		
		foreach (var card in Tabs.Children.OfType<TabCard>())
		{
			if (card.Key == oldActiveTab)
				card.ToggleActiveTo(false);
			else if (card.Key == newActiveTab)
				card.ToggleActiveTo(true);
		}
	};
	
	await TabManager.InitializeData();
	
	_homePage = new HomePage();
	await _homePage.Initialize(TabManager);
	TabHolder.Children.Add(_homePage);
	TabManager.SwapActiveTabTo(-1);

	_homePage.ToggleEditMode += ToggleHomeEdit;

	TabManager.TabCreated += tab =>
	{
	    var tabCard = new TabCard(tab);
	    
	    TabHolder.Children.Add(tab.TabCore);

	    tabCard.DragPositionChanged += OnTabCardDragChanged;
	    
	    tab.UrlChanged += () =>
	    {
	        if (tab.TabId == TabManager.ActiveTabId)
	        {
	            SearchBox.Text = tab.TabCore.Source.ToString();
	            BackButton.Foreground = tab.TabCore.CanGoBack
	                ? new SolidColorBrush(Colors.White)
	                : new SolidColorBrush(Color.FromRgb(100, 100, 100));
	            ForwardButton.Foreground = tab.TabCore.CanGoForward
	                ? new SolidColorBrush(Colors.White)
	                : new SolidColorBrush(Color.FromRgb(100, 100, 100));
	        }

	        if (OwnerInstance.BrowserData.PinInfo.GetAllTabInfos().Any(t => t.Value.Url == tab.TabCore.Source.ToString()))
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
	        tabCard.TitleLabel.Content = tab.Title;
	    };

	    tab.ImageChanged += () =>
	    {
	        tabCard.TabIcon.Child = tab.Icon;
	    };

	    tab.NewTabRequested += (uri) =>
	    {
		    TabManager.SwapActiveTabTo(TabManager.AddTab(uri));
	    };

	    tabCard.CardClicked += () => TabManager.SwapActiveTabTo(tab.TabId);
	    tabCard.RemoveRequested += () => TabManager.RemoveTab(tab.TabId);
	    tabCard.DuplicateRequested += () =>
	    {
		    TabManager.SwapActiveTabTo(TabManager.AddTab(tab.TabCore.Source.ToString()));
	    };

	    Tabs.Children.Add(tabCard);
	};

	TabManager.TabRemoved += id =>
	{
	    var card = Tabs.Children.OfType<TabCard>().FirstOrDefault(tc => tc.Key == id);
	    if (card != null)
	        Tabs.Children.Remove(card);
	};

	OwnerInstance.BrowserData.PinInfo.TabInfoAdded += (key, pin) =>
	{
		var tabCard = new TabCard(key, pin);

		tabCard.CardClicked += () => TabManager.SwapActiveTabTo(TabManager.AddTab(pin.Url));
		tabCard.RemoveRequested += () => OwnerInstance.BrowserData.PinInfo.RemoveTabInfo(key);

		PinnedTabs.Children.Add(tabCard);
	};

	OwnerInstance.BrowserData.PinInfo.TabInfoRemoved += (id, _) =>
	{
		var card = PinnedTabs.Children.OfType<TabCard>().FirstOrDefault(tc => tc.Key == id);
		
		if (card != null)
			PinnedTabs.Children.Remove(card);
		
		var tab = TabManager.GetTab(TabManager.ActiveTabId);
		
		if (OwnerInstance.BrowserData.PinInfo.GetAllTabInfos().Any(t => t.Value.Url == (tab?.TabCore?.Source?.ToString()?? "__NULL__")))
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

	var e = OwnerInstance.BrowserData.PinInfo.GetAllTabInfos();
	PinnedTabs.Children.Clear();
	foreach (var pinKeyValue in OwnerInstance.BrowserData.PinInfo.GetAllTabInfos())
	{
		var tabCard = new TabCard(pinKeyValue.Key, pinKeyValue.Value);

		tabCard.CardClicked += () => TabManager.SwapActiveTabTo(TabManager.AddTab(pinKeyValue.Value.Url));
		tabCard.RemoveRequested += () => OwnerInstance.BrowserData.PinInfo.RemoveTabInfo(pinKeyValue.Key);
		
		PinnedTabs.Children.Add(tabCard);
	}
}

private List<UIElement> _prevTopLPanelControls = [];
private List<UIElement> _prevBottomLPanelControls = [];

private void ToggleHomeEdit(bool editing)
{
	if (editing)
	{
		_prevTopLPanelControls.Clear();
		_prevTopLPanelControls.AddRange(Tabs.Children.Cast<UIElement>());
		_prevBottomLPanelControls.Clear();
		_prevBottomLPanelControls.AddRange(PinnedTabs.Children.Cast<UIElement>());

		Tabs.Children.Clear();
		PinnedTabs.Children.Clear();

		var widgetOptions = _homePage.GetWidgetOptions();
		var homeOptions = _homePage.GetHomeOptions();

		foreach (var wo in widgetOptions)
		{
			var card = new TabCard(wo.preview, wo.name);
			card.CardClicked += () => _homePage.AddWidget(wo.name);
			Tabs.Children.Add(card);
		}

		PinnedTabs.Children.Add(new Border
		{
			Height = 1,
			Margin = new Thickness(5,0,5,0),
			Background = Brushes.White,
			HorizontalAlignment = HorizontalAlignment.Stretch
		});
		foreach (var ho in homeOptions)
		{
			var card = new TabCard(ho.icon, ho.name);
			card.CardClicked += async () => await _homePage.OptionClicked(ho.type);
			PinnedTabs.Children.Add(card);
		}
	}
	else
	{
		Tabs.Children.Clear();
		foreach (var c in _prevTopLPanelControls)
			Tabs.Children.Add(c);
		PinnedTabs.Children.Clear();
		foreach (var c in _prevBottomLPanelControls)
			PinnedTabs.Children.Add(c);	
	}
}

private async void Search_Click(object? s, EventArgs e)
	{
		try
		{
			if (TabManager.GetTab(TabManager.ActiveTabId) is not { } tab) return;
		
			var tabCore = tab.TabCore;
			await tabCore.EnsureCoreWebView2Async(TabManager.WebsiteEnvironment);

			try
			{
				tabCore.CoreWebView2.Navigate(SearchBox.Text);
			}
			catch (Exception exception)
			{
				tabCore.CoreWebView2.Navigate($"https://www.google.com/search?q={SearchBox.Text}");
			}
		}
		catch (Exception ex)
		{
			// TODO handle exception
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
	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount == 2)
		{
			MaximizeRestore_Click(null, EventArgs.Empty);
		}
		else
		{
			if (_inFullscreen)
				ExitFullscreen(true);

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

	private void RefreshButton_OnClick_Click(object sender, RoutedEventArgs e)
	{
		TabManager.GetTab(TabManager.ActiveTabId)?.TabCore.Reload();
	}

	private void BackButton_OnClick(object sender, RoutedEventArgs e)
	{
		TabManager.GetTab(TabManager.ActiveTabId)?.TabCore.GoBack();
	}

	private void ForwardButton_OnClick(object sender, RoutedEventArgs e)
	{
		TabManager.GetTab(TabManager.ActiveTabId)?.TabCore.GoForward();
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

	private void ExitFullscreen(bool moveToMouse = false)
	{
		try
		{
			_inFullscreen = false;

			if (moveToMouse)
			{
				// Get mouse position relative to this window, then convert to screen coordinates
				var mousePos = Mouse.GetPosition(this);
				mousePos = PointToScreen(mousePos);

				// Convert from device pixels to DIPs using our presentation source magic
				var source = PresentationSource.FromVisual(this);
				mousePos = source?.CompositionTarget?.TransformFromDevice.Transform(mousePos)?? mousePos;

				// Calculate the mouse's relative position within the current window
				var relX = (mousePos.X - this.Left) / this.Width;
				var relY = (mousePos.Y - this.Top) / this.Height;

				// Restore original window size
				Width = _originalRectangle.Width;
				Height = _originalRectangle.Height;

				// Reposition so the mouse is at the same spot on the window
				Left = mousePos.X - (relX * this.Width);
				Top = mousePos.Y - (relY * this.Height);
			}
			else
			{
				// Normal restore without the flirty mouse magic
				Left = _originalRectangle.Left;
				Top = _originalRectangle.Top;
				Width = _originalRectangle.Width;
				Height = _originalRectangle.Height;
			}

			ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.Fullscreen };
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private Rect GetCurrentScreen()
	{
		try
		{
			var hWnd = new WindowInteropHelper(this).Handle;
			var hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);

			var monitorInfo = new MONITORINFO
			{
				cbSize = Marshal.SizeOf<MONITORINFO>()
            };

			if (GetMonitorInfo(hMonitor, ref monitorInfo))
			{
				// Get raw bounds in device pixels
				var rawRect = new Rect(
					monitorInfo.rcMonitor.Left,
					monitorInfo.rcMonitor.Top,
					monitorInfo.rcMonitor.Width,
					monitorInfo.rcMonitor.Height);

				// Convert to DIPs so everything stays in sync
				var source = PresentationSource.FromVisual(this);
				if (source?.CompositionTarget != null)
				{
					var transform = new MatrixTransform(source.CompositionTarget.TransformFromDevice);
					var dipRect = transform.TransformBounds(rawRect);
					return dipRect;
				}
				return rawRect;
			}

			// Fallback: if monitor info fails, use the primary monitor's work area
			return SystemParameters.WorkArea with { X = 0, Y = 0 };
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
	            var mainWindow = senderRect?.Tag as Window;
	            if (senderRect != null && mainWindow != null)
	            {
	                // Get the current mouse position relative to the main window
	                Point pos = e.GetPosition(mainWindow);
	
	                // Start with current window values
	                double newLeft = mainWindow.Left;
	                double newTop = mainWindow.Top;
	                double newWidth = mainWindow.Width;
	                double newHeight = mainWindow.Height;
	
	                // Check which sides are being resized
	                bool resizeLeft = senderRect.Name.ToLower().Contains("left");
	                bool resizeRight = senderRect.Name.ToLower().Contains("right");
	                bool resizeTop = senderRect.Name.ToLower().Contains("top");
	                bool resizeBottom = senderRect.Name.ToLower().Contains("bottom");
	
	                // Process left resizing: adjust newLeft and newWidth
	                if (resizeLeft)
	                {
	                    // pos.X is the new distance from the left edge
	                    double deltaX = pos.X;
	                    double proposedWidth = mainWindow.Width - deltaX;
	                    if (proposedWidth >= mainWindow.MinWidth)
	                    {
	                        newLeft += deltaX;
	                        newWidth = proposedWidth;
	                    }
	                }
	
	                // Process right resizing: new width based on mouse position
	                if (resizeRight)
	                {
	                    // pos.X gives the new width from the left edge
	                    double proposedWidth = pos.X + 1; // adding a little offset
	                    if (proposedWidth >= mainWindow.MinWidth)
	                    {
	                        newWidth = proposedWidth;
	                    }
	                }
	
	                // Process top resizing: adjust newTop and newHeight
	                if (resizeTop)
	                {
	                    double deltaY = pos.Y;
	                    double proposedHeight = mainWindow.Height - deltaY;
	                    if (proposedHeight >= mainWindow.MinHeight)
	                    {
	                        newTop += deltaY;
	                        newHeight = proposedHeight;
	                    }
	                }
	
	                // Process bottom resizing: new height based on mouse position
	                if (resizeBottom)
	                {
	                    double proposedHeight = pos.Y + 1; // little offset
	                    if (proposedHeight >= mainWindow.MinHeight)
	                    {
	                        newHeight = proposedHeight;
	                    }
	                }
	
	                // Apply the computed values
	                mainWindow.Left = newLeft;
	                mainWindow.Top = newTop;
	                mainWindow.Width = newWidth;
	                mainWindow.Height = newHeight;
	            }
	        }
	    }
	    catch (Exception ex)
	    {
	        MessageBox.Show(ex.Message, $"Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
	    }
	}

	private void ResizeHandle_MouseEnter(object sender, MouseEventArgs e)
	{
		if (sender is Rectangle handle)
		{
			// If we're in fullscreen, override the cursor to the default arrow
			if (_inFullscreen)
			{
				handle.Cursor = Cursors.Arrow;
			}
			// Otherwise, you can optionally re-set the cursor based on the handle's original intent.
		}
	}

	private void ResizeHandle_MouseLeave(object sender, MouseEventArgs e)
	{
		if (sender is not Rectangle handle) return;
		// Reset the cursor to its original value based on the handle's name
		var name = handle.Name.ToLower();
		if (name.Contains("left") || name.Contains("right"))
			handle.Cursor = Cursors.SizeWE;
		else if (name.Contains("top") || name.Contains("bottom"))
			handle.Cursor = Cursors.SizeNS;
		else if (name.Contains("topleft") || name.Contains("bottomright"))
			handle.Cursor = Cursors.SizeNWSE;
		else if (name.Contains("topright") || name.Contains("bottomleft"))
			handle.Cursor = Cursors.SizeNESW;
		else
			handle.Cursor = Cursors.Arrow;
	}
	#endregion
}