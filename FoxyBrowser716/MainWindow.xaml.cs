
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
using Microsoft.Web.WebView2.Core;
using static FoxyBrowser716.Styling.ColorPalette;
using static FoxyBrowser716.Styling.Animator;


namespace FoxyBrowser716;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private HomePage _homePage;

	private bool _inFullscreen;

	private System.Windows.Rect _originalRectangle;
	
	internal readonly TabManager TabManager;

	private InstanceManager _instanceData;

	internal Task InitTask;
	
	public MainWindow(InstanceManager instanceData)
	{
		_instanceData = instanceData;
			
		InitializeComponent();

		TabManager = new TabManager(_instanceData);
		InitTask = Initialize();
		
		//TODO: find a better way to do all this crazy GUI stuff
		foreach (var button in NormalButtons)
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

			if (_instanceData.PinInfo.GetAllTabInfos().Any(t => t.Value.Url == (tab?.TabCore?.Source?.ToString()??"__NULL__")))
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
				LabelPin.Content = "Pin Tab";
				_instanceData.PinInfo.RemoveTabInfo(_instanceData.PinInfo.GetAllTabInfos()
					.FirstOrDefault(p => p.Value.Url == tab.TabCore.Source.ToString()).Key);
			}
			else
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.Pin };
				LabelPin.Content = "Unpin Tab";
				_instanceData.PinInfo.AddTabInfo(tab);
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
			
			var tab = TabManager.GetTab(TabManager.ActiveTabId);
			
			if (tab is null) return;

			if (_instanceData.BookmarkInfo.GetAllTabInfos().Any(t => t.Value.Url == (tab?.TabCore?.Source?.ToString()??"__NULL__")))
			{
				ButtonBookmark.Content = new MaterialIcon { Kind = MaterialIconKind.BookmarkOutline };
				LabelBookmark.Content = "Add Bookmark";
				_instanceData.BookmarkInfo.RemoveTabInfo(_instanceData.BookmarkInfo.GetAllTabInfos()
					.FirstOrDefault(p => p.Value.Url == tab.TabCore.Source.ToString()).Key);
			}
			else
			{
				ButtonBookmark.Content = new MaterialIcon { Kind = MaterialIconKind.Bookmark };
				LabelBookmark.Content = "Remove bookmark";
				_instanceData.BookmarkInfo.AddTabInfo(tab);
			}
		};

		LeftBar.MouseEnter += LeftBarMouseEnter;
		LeftBar.MouseLeave += LeftBarMouseLeave;

		SearchBox.KeyDown += (_, e) =>
		{
			if (e.Key == Key.Enter)
			{
				Search_Click(null, EventArgs.Empty);
			}
		};
	}

	private bool _mouseOverSideBar;
	private void LeftBarMouseEnter(object sender, MouseEventArgs e)
	{
		_mouseOverSideBar = true;
		Task.Delay(150).ContinueWith(_ =>
		{
			if (_mouseOverSideBar && !SideOpen)
				Dispatcher.Invoke(OpenSideBar);
		});
	}
	
	private void LeftBarMouseLeave(object sender, MouseEventArgs e)
	{
		_mouseOverSideBar = false;
		Task.Delay(250).ContinueWith(_ =>
		{
			if (!_mouseOverSideBar && SideOpen)
				Dispatcher.Invoke(CloseSideBar);
		});
	}

	internal bool SideOpen;
	internal void OpenSideBar()
	{
		if (SideOpen) return;
		
		SideOpen = true;
		var animation = new DoubleAnimation
		{
			Duration = TimeSpan.FromSeconds(0.2),
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
		if (!SideOpen) return;
		
		SideOpen = false;
		var animation = new DoubleAnimation
		{
			Duration = TimeSpan.FromSeconds(0.3),
			To = 30,
			EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			}
		};

		LeftBar.BeginAnimation(WidthProperty, animation);
	}

	internal System.Windows.Rect GetLeftBarDropArea()
	{
		var p = PointToScreen(new Point(0, 30));
		return new System.Windows.Rect(p.X, p.Y, 260, LeftBar.ActualHeight);
	}

	private Control[] NormalButtons =>
	[
		ButtonMaximize,
		ButtonMinimize,
		ButtonMenu,
		SearchButton,
		RefreshButton,
		BackButton, //TODO: need custom animation logic.
		ForwardButton
	];
	
	private ContextMenu _menu = new()
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
	private ContextMenu _secondaryMenu = new()
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

	private void ButtonMenu_Click(object sender, RoutedEventArgs e)
	{
		if (_menu is { IsOpen: true })
		{
			_menu.IsOpen = false;
			return;
		}

		Dictionary<string, Action> menuItems = TabManager.ActiveTabId != -1
			? new()
			{
				{ "Settings", OpenSettings },
				{ "Bookmarks", OpenBookmarks },
				{ "History", OpenHistory },
				{ "Downloads", OpenDownloads },
				{ "Instances", OpenInstances },
				{ "Extensions", OpenExtensions }
			}
			: new()
			{
				{ "Settings", OpenSettings },
				{ "Bookmarks", OpenBookmarks },
				{ "History", OpenHistory },
				{ "Instances", OpenInstances },
			};
		
		ApplyMenuItems(_menu, menuItems);
		
		_menu.PlacementTarget = (Button)sender;
		_menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
		_menu.IsOpen = true;
	}

	private void ApplyMenuItems(ContextMenu menu, Dictionary<string, Action> items)
	{
		menu.Items.Clear();
		foreach (var menuItem in items)
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
			menu.Items.Add(item);
		}
	}

private void OpenSettings()
{
	throw new NotImplementedException();
}

private void OpenInstances()
{
	var openNewWindow = TabManager.GetAllTabs().Count > 0;
	var instanceOptions = ServerManager.Context.AllBrowserManagers
		.Where(i => i.InstanceName != _instanceData.InstanceName)
		.ToDictionary<InstanceManager, string, Action>(i => $"{(openNewWindow ? "Open" : "Swap To")} {i.InstanceName}",
			i => async () =>
			{
				if (openNewWindow)
					await ServerManager.Context.CreateWindow(null, null, i);
				else
				{
					await ServerManager.Context.CreateWindow(new System.Windows.Rect(Left, Top, Width, Height), _inFullscreen, i);
					Close();
				}
			});
	instanceOptions.Add("Manage Instances", () =>
	{
		throw new NotImplementedException();
	});
	ApplyMenuItems(_secondaryMenu, instanceOptions);
	_secondaryMenu.PlacementTarget = ButtonMenu;
	_secondaryMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
	_secondaryMenu.IsOpen = true;
}

private void OpenBookmark(string url)
{
	TabManager.SwapActiveTabTo(TabManager.AddTab(url));
}

private void OpenBookmarks()
{
	ApplyMenuItems(_secondaryMenu, _instanceData.BookmarkInfo.GetAllTabInfos().Values
		.ToDictionary<TabInfo, string, Action>(k => k.Title, k => () => OpenBookmark(k.Url))
	);
	_secondaryMenu.PlacementTarget = ButtonMenu;
	_secondaryMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
	_secondaryMenu.IsOpen = true;
}

private void OpenHistory()
{
}

private void OpenDownloads()
{
	var tabCoreRaw = TabManager.GetTab(TabManager.ActiveTabId)?.TabCore.CoreWebView2;
	if (tabCoreRaw is not { } tabCore) return;
	
	if (tabCore.IsDefaultDownloadDialogOpen) tabCore.CloseDefaultDownloadDialog();
	else tabCore.OpenDefaultDownloadDialog();
}

private async void OpenExtensions()
{
	if (TabManager.GetTab(TabManager.ActiveTabId) is not { } tab) return;

	var extensions = await tab.TabCore.CoreWebView2.Profile.GetBrowserExtensionsAsync();
	ApplyMenuItems(_secondaryMenu, extensions
		.ToDictionary<CoreWebView2BrowserExtension, string, Action>(k => $"{ k.Id } - { k.Name }",
			k => () => throw new NotImplementedException())
	);
	_secondaryMenu.PlacementTarget = ButtonMenu;
	_secondaryMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
	_secondaryMenu.IsOpen = true;
}

private async void OnTabCardDragChanged(TabCard sender, int? relativeMove)
{
	try
	{
		if (relativeMove == null)
		{
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
	var tabCardWindow = new TabMoveWindowCard(tab)
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
	await _homePage.Initialize(TabManager, _instanceData);
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

	        if (_instanceData.PinInfo.GetAllTabInfos().Any(t => t.Value.Url == tab.TabCore.Source.ToString()))
	        {
	            ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.Pin };
	            LabelPin.Content = "Unpin Tab";
	        }
	        else
	        {
	            ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
	            LabelPin.Content = "Pin Tab";
	        }
	        
	        if (_instanceData.BookmarkInfo.GetAllTabInfos().Any(t => t.Value.Url == tab.TabCore.Source.ToString()))
	        {
		        ButtonBookmark.Content = new MaterialIcon { Kind = MaterialIconKind.Bookmark };
		        LabelBookmark.Content = "Remove Bookmark";
	        }
	        else
	        {
		        ButtonBookmark.Content = new MaterialIcon { Kind = MaterialIconKind.BookmarkOutline };
		        LabelBookmark.Content = "Add Bookmark";
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
		    TabManager.AddTab(uri);
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

	_instanceData.PinInfo.TabInfoAdded += (key, pin) =>
	{
		var tabCard = new TabCard(key, pin);

		tabCard.CardClicked += () => TabManager.SwapActiveTabTo(TabManager.AddTab(pin.Url));
		tabCard.RemoveRequested += () => _instanceData.PinInfo.RemoveTabInfo(key);

		PinnedTabs.Children.Add(tabCard);
	};

	_instanceData.PinInfo.TabInfoRemoved += (id, _) =>
	{
		var card = PinnedTabs.Children.OfType<TabCard>().FirstOrDefault(tc => tc.Key == id);
		
		if (card != null)
			PinnedTabs.Children.Remove(card);
		
		var tab = TabManager.GetTab(TabManager.ActiveTabId);
		
		if (_instanceData.PinInfo.GetAllTabInfos().Any(t => t.Value.Url == (tab?.TabCore?.Source?.ToString()?? "__NULL__")))
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

	var e = _instanceData.PinInfo.GetAllTabInfos();
	PinnedTabs.Children.Clear();
	foreach (var pinKeyValue in _instanceData.PinInfo.GetAllTabInfos())
	{
		var tabCard = new TabCard(pinKeyValue.Key, pinKeyValue.Value);

		tabCard.CardClicked += () => TabManager.SwapActiveTabTo(TabManager.AddTab(pinKeyValue.Value.Url));
		tabCard.RemoveRequested += () => _instanceData.PinInfo.RemoveTabInfo(pinKeyValue.Key);
		
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
			card.CardClicked += () => _homePage.AddWidgetClicked(wo.name);
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

			_originalRectangle = new System.Windows.Rect(Left, Top, Width, Height);
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
		_inFullscreen = true;
		_originalRectangle = new System.Windows.Rect(Left, Top, Width, Height);

		var screen = GetCurrentScreen();

		Left = screen.Left;
		Top = screen.Top;
		Width = screen.Width;
		Height = screen.Height;
		ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.FullscreenExit };
	}

	private void ExitFullscreen(bool moveToMouse = false)
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

	private System.Windows.Rect GetCurrentScreen()
	{
		var hWnd = new WindowInteropHelper(this).Handle;
		var hMonitor = MonitorFromWindow(hWnd, MonitorDefaulttonearest);

		var monitorInfo = new Monitorinfo
		{
			cbSize = Marshal.SizeOf<Monitorinfo>()
        };

		if (GetMonitorInfo(hMonitor, ref monitorInfo))
		{
			// Get raw bounds in device pixels
			var rawRect = new System.Windows.Rect(
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

	
	#region Windows API Methods

	// Get the monitor's handle from a window handle
	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

	// Get information about a monitor
	[DllImport("user32.dll")]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref Monitorinfo lpmi);

	// Monitor Info Struct
	[StructLayout(LayoutKind.Sequential)]
	public struct Monitorinfo
	{
		public int cbSize;
		public Rect rcMonitor;
		public Rect rcWork;
		public uint dwFlags;
	}

	// Rect struct to define monitor size
	[StructLayout(LayoutKind.Sequential)]
	public struct Rect
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public int Width => Right - Left;
		public int Height => Bottom - Top;
	}

	// Constants
	private const uint MonitorDefaulttonearest = 0x00000002;

	#endregion

	#endregion

	#region ResizeWindows

	private bool _resizeInProcess;
	private void Resize_Init(object sender, MouseButtonEventArgs e)
	{
		var senderRect = sender as Rectangle;
		if (senderRect != null && !_inFullscreen)//TODO: verify that this works
		{
			_resizeInProcess = true;
			senderRect.CaptureMouse();
		}
	}

	private void Resize_End(object sender, MouseButtonEventArgs e)
	{
		var senderRect = sender as Rectangle;
		if (senderRect != null)
		{
			_resizeInProcess = false;
			senderRect.ReleaseMouseCapture();
		}
	}

	private void Resizeing_Form(object sender, MouseEventArgs e)
	{
        if (_resizeInProcess)
        {
            var senderRect = sender as Rectangle;
            var mainWindow = senderRect?.Tag as Window;
            if (senderRect != null && mainWindow != null)
            {
                // Get the current mouse position relative to the main window
                var pos = e.GetPosition(mainWindow);

                // Start with current window values
                var newLeft = mainWindow.Left;
                var newTop = mainWindow.Top;
                var newWidth = mainWindow.Width;
                var newHeight = mainWindow.Height;

                // Check which sides are being resized
                var resizeLeft = senderRect.Name.ToLower().Contains("left");
                var resizeRight = senderRect.Name.ToLower().Contains("right");
                var resizeTop = senderRect.Name.ToLower().Contains("top");
                var resizeBottom = senderRect.Name.ToLower().Contains("bottom");

                // Process left resizing: adjust newLeft and newWidth
                if (resizeLeft)
                {
                    // pos.X is the new distance from the left edge
                    var deltaX = pos.X;
                    var proposedWidth = mainWindow.Width - deltaX;
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
                    var proposedWidth = pos.X + 1; // adding a little offset
                    if (proposedWidth >= mainWindow.MinWidth)
                    {
                        newWidth = proposedWidth;
                    }
                }

                // Process top resizing: adjust newTop and newHeight
                if (resizeTop)
                {
                    var deltaY = pos.Y;
                    var proposedHeight = mainWindow.Height - deltaY;
                    if (proposedHeight >= mainWindow.MinHeight)
                    {
                        newTop += deltaY;
                        newHeight = proposedHeight;
                    }
                }

                // Process bottom resizing: new height based on mouse position
                if (resizeBottom)
                {
                    var proposedHeight = pos.Y + 1; // little offset
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