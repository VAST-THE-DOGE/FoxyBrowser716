using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using FoxyBrowser716.Settings;
using Material.Icons;
using Material.Icons.WPF;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using static FoxyBrowser716.Styling.ColorPalette;
using static FoxyBrowser716.Styling.Animator;
using Path = System.IO.Path;

namespace FoxyBrowser716;

public partial class BrowserApplicationWindow : Window
{
	private HomePage _homePage;
	private SettingsPage _settingsPage;

	private bool _inFullscreen;

	private List<ExtensionPopupWindow> _extensionPopups = [];

	private System.Windows.Rect _originalRectangle;

	internal readonly TabManager TabManager;

	private InstanceManager _instanceData;

	internal Task InitTask;
	
	private Timer _blurUpdateTimer;
	
	public BrowserApplicationWindow(InstanceManager instanceData)
	{
		InitializeComponent();
		SetupAnimationsAndColors();
		
		_instanceData = instanceData;
		TabManager = new TabManager(_instanceData);
		InitTask = Initialize();

		_blurUpdateTimer = new Timer((_) =>
		{
			Dispatcher.Invoke(UpdateBlurredBackground);
		}, null, 0, 100);

		SearchButton.Click += async (s, e) => await Search_Click(s, e);

		//StateChanged += Window_StateChanged;
		//Window_StateChanged(null, EventArgs.Empty);
		
		ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.Fullscreen };

		AddTabStack.MouseEnter += (_, _) =>
		{
			ChangeColorAnimation(AddTabStack.Background, _transparentBack, _transparentAccent);
		};
		AddTabStack.MouseLeave += (_, _) =>
		{
			ChangeColorAnimation(AddTabStack.Background, _transparentAccent, _transparentBack);
		};
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

		StackPin.MouseEnter += (_, _) => { ChangeColorAnimation(StackPin.Background, _transparentBack, _transparentAccent); };
		StackPin.MouseLeave += (_, _) => { ChangeColorAnimation(StackPin.Background, _transparentAccent, _transparentBack); };
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

			if (_instanceData.PinInfo.GetAllTabInfos()
			    .Any(t => t.Value.Url == (tab?.TabCore?.Source?.ToString() ?? "__NULL__")))
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
				StackPin.BorderBrush = Brushes.White;
				LabelPin.Content = "Pin Tab";
				_instanceData.PinInfo.RemoveTabInfo(_instanceData.PinInfo.GetAllTabInfos()
					.FirstOrDefault(p => p.Value.Url == tab.TabCore.Source.ToString()).Key);
			}
			else
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.Pin };
				StackPin.BorderBrush = Brushes.White;
				LabelPin.Content = "Unpin Tab";
				_instanceData.PinInfo.AddTabInfo(tab);
			}
		};

		StackBookmark.MouseEnter += (_, _) =>
		{
			ChangeColorAnimation(StackBookmark.Background, _transparentBack, _transparentAccent);
		};
		StackBookmark.MouseLeave += (_, _) =>
		{
			ChangeColorAnimation(StackBookmark.Background, _transparentAccent, _transparentBack);
		};
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

			if (_instanceData.BookmarkInfo.GetAllTabInfos()
			    .Any(t => t.Value.Url == (tab?.TabCore?.Source?.ToString() ?? "__NULL__")))
			{
				ButtonBookmark.Content = new MaterialIcon { Kind = MaterialIconKind.BookmarkOutline };
				StackBookmark.BorderBrush = new SolidColorBrush(MainColor);
				LabelBookmark.Content = "Add Bookmark";
				_instanceData.BookmarkInfo.RemoveTabInfo(_instanceData.BookmarkInfo.GetAllTabInfos()
					.FirstOrDefault(p => p.Value.Url == tab.TabCore.Source.ToString()).Key);
			}
			else
			{
				ButtonBookmark.Content = new MaterialIcon { Kind = MaterialIconKind.Bookmark };
				StackBookmark.BorderBrush = Brushes.White;
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

		Closed += (_, _) =>
		{
			var tabs = TabManager.GetAllTabs();
			foreach (var t in tabs)
			{
				TabManager.RemoveTab(t.Key);
			}
		};
	}
	
	private void UpdateBlurredBackground()
	{
		if (TabHolder.ActualWidth > 0 && TabHolder.ActualHeight > 0)
		{
			var visualBrush = new VisualBrush(TabHolder)
			{
				Stretch = Stretch.UniformToFill,
				TileMode = TileMode.None,
				Transform = new ScaleTransform(1.2, 1.2, 0.5, 0.5)
			};
        
			BlurredBackground.Background = visualBrush;
			BlurredBackground.Effect = new BlurEffect { Radius = 25 };
			BlurredBackground.Opacity = 0.5;
		}
	}

	
	private async Task Search_Click(object? s, EventArgs e)
	{
		if (TabManager.GetTab(TabManager.ActiveTabId) is not { } tab) return;

		var tabCore = tab.TabCore;
		await tabCore.EnsureCoreWebView2Async(TabManager.WebsiteEnvironment);

		try
		{
			tabCore.CoreWebView2.Navigate(SearchBox.Text);
		}
		catch
		{
			tabCore.CoreWebView2.Navigate($"https://www.google.com/search?q={Uri.EscapeDataString(SearchBox.Text)}");
		}
	}
	
	private readonly Color _transparentBack = Color.FromArgb(175, 48, 50, 58);
	private readonly Color _transparentAccent = Color.FromArgb(200, AccentColor.R, AccentColor.G, AccentColor.B);
	private void SetupAnimationsAndColors(/*TODO*/)
	{
		var hoverColor = Color.FromArgb(50,255,255,255);
		var closehover = Color.FromArgb(150,255,0,0);
		
		AddTabStack.BorderBrush = new SolidColorBrush(MainColor);
		StackPin.BorderBrush = new SolidColorBrush(MainColor);
		StackBookmark.BorderBrush = new SolidColorBrush(MainColor);
		
		ButtonClose.MouseEnter += (_, _) => { ChangeColorAnimation(ButtonClose.Background, Colors.Transparent, closehover); };
		ButtonClose.MouseLeave += (_, _) => { ChangeColorAnimation(ButtonClose.Background, closehover, Colors.Transparent); };

		foreach (var b in (Button[])
		         [
			         ButtonMaximize, ButtonMinimize, ButtonMenu,
			         BackButton, ForwardButton, RefreshButton,
			         SearchButton,
		         ])
		{
			b.MouseEnter += (_, _) => { ChangeColorAnimation(b.Background, Colors.Transparent, hoverColor); };
			b.MouseLeave += (_, _) => { ChangeColorAnimation(b.Background, hoverColor, Colors.Transparent); };
			
			b.PreviewMouseUp += (_, _) => { ChangeColorAnimation(b.Foreground, HighlightColor, Colors.White); };
			b.PreviewMouseDown += (_, _) => { ChangeColorAnimation(b.Foreground, Colors.White, HighlightColor); };
		}
	}
	
	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		UpdatePlaceholderVisibility();
	}
    
	private void TextBox_GotFocus(object sender, RoutedEventArgs e)
	{
		UpdatePlaceholderVisibility();
	}
    
	private void TextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		UpdatePlaceholderVisibility();
	}
	
	private void UpdatePlaceholderVisibility()
	{
		placeholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text) && !SearchBox.IsFocused 
			? Visibility.Visible 
			: Visibility.Collapsed;
	}

	
	#region ResizeWindows

	private bool _resizeInProcess;

	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount == 2)
		{
			// MaximizeRestore_Click(null, EventArgs.Empty);
		}
		else
		{
			// if (_inFullscreen)
			// 	ExitFullscreen(true);
			//
			// _originalRectangle = new System.Windows.Rect(Left, Top, Width, Height);
			DragMove();
		}
	}
	
	private void Resize_Init(object sender, MouseButtonEventArgs e)
	{
		var senderRect = sender as Rectangle;
		if (senderRect != null /*&& !_inFullscreen*/) //TODO: verify that this works
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
				var pos = e.GetPosition(mainWindow);

				var newLeft = mainWindow.Left;
				var newTop = mainWindow.Top;
				var newWidth = mainWindow.Width;
				var newHeight = mainWindow.Height;

				var resizeLeft = senderRect.Name.ToLower().Contains("left");
				var resizeRight = senderRect.Name.ToLower().Contains("right");
				var resizeTop = senderRect.Name.ToLower().Contains("top");
				var resizeBottom = senderRect.Name.ToLower().Contains("bottom");

				if (resizeLeft)
				{
					var deltaX = pos.X;
					var proposedWidth = mainWindow.Width - deltaX;
					if (proposedWidth >= mainWindow.MinWidth)
					{
						newLeft += deltaX;
						newWidth = proposedWidth;
					}
				}

				if (resizeRight)
				{
					var proposedWidth = pos.X + 1;
					if (proposedWidth >= mainWindow.MinWidth)
					{
						newWidth = proposedWidth;
					}
				}

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

				if (resizeBottom)
				{
					var proposedHeight = pos.Y + 1;
					if (proposedHeight >= mainWindow.MinHeight)
					{
						newHeight = proposedHeight;
					}
				}

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
			// if (_inFullscreen)
			// {
			// 	handle.Cursor = Cursors.Arrow;
			// }
		}
	}

	private void ResizeHandle_MouseLeave(object sender, MouseEventArgs e)
	{
		// if (sender is not Rectangle handle) return;
		// var name = handle.Name.ToLower();
		// if (name.Contains("left") || name.Contains("right"))
		// 	handle.Cursor = Cursors.SizeWE;
		// else if (name.Contains("top") || name.Contains("bottom"))
		// 	handle.Cursor = Cursors.SizeNS;
		// else if (name.Contains("topleft") || name.Contains("bottomright"))
		// 	handle.Cursor = Cursors.SizeNWSE;
		// else if (name.Contains("topright") || name.Contains("bottomleft"))
		// 	handle.Cursor = Cursors.SizeNESW;
		// else
		// 	handle.Cursor = Cursors.Arrow;
	}

	#endregion
	
	private bool _mouseOverSideBar;

	private void LeftBarMouseEnter(object sender, MouseEventArgs e)
	{
		_mouseOverSideBar = true;
		var click = false;

		void LeftDown(object sender, MouseButtonEventArgs e)
		{
			click = true;
		}
		PreviewMouseLeftButtonDown += LeftDown;
		
		Task.Delay(250).ContinueWith(_ =>
		{
			PreviewMouseLeftButtonDown -= LeftDown;
			if (_mouseOverSideBar && !SideOpen && !click)
				Dispatcher.Invoke(OpenSideBar);
		});
	}

	private void LeftBarMouseLeave(object sender, MouseEventArgs e)
	{
		_mouseOverSideBar = false;
		Task.Delay(300).ContinueWith(_ =>
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
			Duration = TimeSpan.FromSeconds(0.4),
			To = 260,
			EasingFunction = new ExponentialEase
			{
				EasingMode = EasingMode.EaseOut,
				Exponent = 8
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
			Duration = TimeSpan.FromSeconds(0.4),
			To = 30,
			EasingFunction = new ExponentialEase
			{
				EasingMode = EasingMode.EaseIn,
				Exponent = 6
			}
		};

		LeftBar.BeginAnimation(WidthProperty, animation);
	}

	internal System.Windows.Rect GetLeftBarDropArea()
	{
		var p = PointToScreen(new Point(0, 30));
		return new System.Windows.Rect(p.X, p.Y, 260, LeftBar.ActualHeight);
	}

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

		Dictionary<string, Action> menuItems = TabManager.ActiveTabId >= 0
			? new()
			{
				{ "Settings", OpenSettings },
				{ "Bookmarks", OpenBookmarks },
				{ "History", OpenHistory },
				{ "Downloads", OpenDownloads },
				{ "Instances", OpenInstances },
				{ "Extensions", async () => await OpenExtensions() }
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
		TabManager.SwapActiveTabTo(-2);
	}

	private void OpenInstances()
	{
		var openNewWindow = TabManager.GetAllTabs().Count > 0;
		var instanceOptions = ServerManager.Context.AllBrowserManagers
			.Where(i => i.InstanceName != _instanceData.InstanceName)
			.ToDictionary<InstanceManager, string, Action>(
				i => $"{(openNewWindow ? "Open" : "Swap To")} {i.InstanceName}",
				i => async () =>
				{
					if (openNewWindow)
						await ServerManager.Context.CreateWindow(null, null, i);
					else
					{
						await ServerManager.Context.CreateWindow(new System.Windows.Rect(Left, Top, Width, Height),
							_inFullscreen, i);
						Close();
					}
				});
		instanceOptions.Add("Manage Instances", () => throw new NotImplementedException());
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
		var tier = RenderCapability.Tier >> 16;
		switch (tier)
		{
			case 0:
				Console.WriteLine("No hardware acceleration (rendering tier 0).");
				break;
			case 1:
				Console.WriteLine("Partial hardware acceleration (rendering tier 1).");
				break;
			case 2:
				Console.WriteLine("Full hardware acceleration (rendering tier 2).");
				break;
		}
		//throw new NotImplementedException();
	}

	private void OpenDownloads()
	{
		var tabCoreRaw = TabManager.GetTab(TabManager.ActiveTabId)?.TabCore.CoreWebView2;
		if (tabCoreRaw is not { } tabCore) return;

		if (tabCore.IsDefaultDownloadDialogOpen) tabCore.CloseDefaultDownloadDialog();
		else tabCore.OpenDefaultDownloadDialog();
	}

	private static string FindPopupHtml(string manifest, string extensionPath)
	{
		try
		{
			var manifestContent = File.ReadAllText(manifest);

			var doc = JsonDocument.Parse(manifestContent);
			var root = doc.RootElement;

			string popupRelativePath = null;

			if (root.TryGetProperty("browser_action", out var browserAction))
			{
				if (browserAction.TryGetProperty("default_popup", out var popup))
				{
					popupRelativePath = popup.GetString();
				}
			}
			else if (root.TryGetProperty("page_action", out var pageAction))
			{
				if (pageAction.TryGetProperty("default_popup", out var popup))
				{
					popupRelativePath = popup.GetString();
				}
			}
			else if (root.TryGetProperty("action", out var action))
			{
				if (action.TryGetProperty("default_popup", out var popup))
				{
					popupRelativePath = popup.GetString();
				}
			}


			if (!string.IsNullOrEmpty(popupRelativePath))
			{
				var fullPath = Path.Combine(extensionPath, popupRelativePath);

				if (File.Exists(fullPath))
				{
					return popupRelativePath;
				}
			}

			return null;
		}
		catch (Exception ex)
		{
			return null;
		}
	}

	private async Task OpenExtensions()
	{
		if (TabManager.GetTab(TabManager.ActiveTabId) is not { } tab) return;

		var extensions = await tab.TabCore.CoreWebView2.Profile.GetBrowserExtensionsAsync();
		ApplyMenuItems(_secondaryMenu, extensions
			.ToDictionary<CoreWebView2BrowserExtension, string, Action>(k => $"{k.Name} ({k.Id})",
				k => async () =>
				{
					if (!tab.Extensions.TryGetValue(k.Id, out var extension)) return;
					if (Directory.GetFiles(extension.Path, "manifest.json", SearchOption.TopDirectoryOnly)
						    .FirstOrDefault() is not { } manifest) return;
					if (FindPopupHtml(manifest, extension.Path) is not { } popup) return;

					var popupUrl = $"chrome-extension://{k.Id}/{popup}";

					//await tab.TabCore.CoreWebView2.ExecuteScriptAsync($"window.open(\"{popupUrl}\", '_blank', 'width=400,height=300');");
					var environment = tab.TabCore.CoreWebView2.Environment;
					var popupWindow = new ExtensionPopupWindow(popupUrl, environment, tab, TabManager);
					_extensionPopups.Add(popupWindow);
					popupWindow.Show();
				})
		);
		_secondaryMenu.PlacementTarget = ButtonMenu;
		_secondaryMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
		_secondaryMenu.IsOpen = true;
	}

	private async Task OnTabCardDragChanged(TabCard sender, int? relativeMove)
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
		//NavigationGrid.Visibility = Visibility.Collapsed;

		TabManager.ActiveTabChanged += (oldActiveTab, newActiveTab) =>
		{
			StackBookmark.Visibility = newActiveTab <= 0 ? Visibility.Collapsed : Visibility.Visible;
			StackPin.Visibility = newActiveTab <= 0 ? Visibility.Collapsed : Visibility.Visible;
			//NavigationGrid.Visibility = newActiveTab <= 0 ? Visibility.Collapsed : Visibility.Visible;
			_homePage.Visibility = newActiveTab == -1 ? Visibility.Visible : Visibility.Collapsed;
			AddTabStack.BorderBrush =
				newActiveTab == -1 ? new SolidColorBrush(HighlightColor) : new SolidColorBrush(MainColor);
			_settingsPage.Visibility = newActiveTab == -2 ? Visibility.Visible : Visibility.Collapsed;

			foreach (var card in Tabs.Children.OfType<TabCard>())
			{
				if (card.Key == oldActiveTab)
					card.ToggleActiveTo(false);
				else if (card.Key == newActiveTab)
					card.ToggleActiveTo(true);
			}

			if (newActiveTab > 0)
			{
				var tab = TabManager.GetTab(newActiveTab);
				SearchBox.Text = tab?.TabCore?.Source?.ToString() ?? "";
			}

			foreach (var popup in _extensionPopups)
			{
				popup._webView2.Dispose();
				popup.Close();
			}
			_extensionPopups.Clear();
		};

		await TabManager.InitializeData();

		_homePage = new HomePage();
		await _homePage.Initialize(TabManager, _instanceData);
		TabHolder.Children.Add(_homePage);


		_settingsPage = PrimarySettingsPage.GeneratePage(null); //TODO
		TabHolder.Children.Add(_settingsPage);

		_settingsPage.Loaded += (_, _) =>
			_settingsPage.Visibility = TabManager.ActiveTabId == -2 ? Visibility.Visible : Visibility.Collapsed;
		_homePage.Loaded += (_, _) =>
			_homePage.Visibility = TabManager.ActiveTabId == -1 ? Visibility.Visible : Visibility.Collapsed;
		AddTabStack.BorderBrush = TabManager.ActiveTabId == -1
			? new SolidColorBrush(HighlightColor)
			: new SolidColorBrush(MainColor);

		TabManager.SwapActiveTabTo(-1);

		_homePage.ToggleEditMode += ToggleHomeEdit;

		TabManager.PreloadCreated += async tab =>
		{
			TabHolder.Children.Add(tab.TabCore);
			tab.NewTabRequested += uri => { TabManager.AddTab(uri); };
			tab.UrlChanged += () =>
			{
				if (tab.TabId == TabManager.ActiveTabId)
				{
					SearchBox.Text = tab.TabCore.Source.ToString();
				}
				
				if (_instanceData.PinInfo.GetAllTabInfos().Any(t => t.Value.Url == tab.TabCore.Source.ToString()))
				{
					ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.Pin };
					StackPin.BorderBrush = Brushes.White;
					LabelPin.Content = "Unpin Tab";
				}
				else
				{
					ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
					StackPin.BorderBrush = new SolidColorBrush(MainColor);
					LabelPin.Content = "Pin Tab";
				}

				if (_instanceData.BookmarkInfo.GetAllTabInfos().Any(t => t.Value.Url == tab.TabCore.Source.ToString()))
				{
					ButtonBookmark.Content = new MaterialIcon { Kind = MaterialIconKind.Bookmark };
					StackBookmark.BorderBrush = Brushes.White;
					LabelBookmark.Content = "Remove Bookmark";
				}
				else
				{
					ButtonBookmark.Content = new MaterialIcon { Kind = MaterialIconKind.BookmarkOutline };
					StackBookmark.BorderBrush = new SolidColorBrush(MainColor);
					LabelBookmark.Content = "Add Bookmark";
				}
			};
			await tab.SetupTask;
			tab.TabCore.CoreWebView2.NewWindowRequested += (_, e) =>
			{
				//if (!e.Uri.StartsWith("chrome-extension://"))
				// if (e.Uri.StartsWith("AS_POPUP:"))
				// {
				// 	// var popupWindow = new ExtensionPopupWindow(e.Uri.Replace("AS_POPUP:",""), tab.TabCore.CoreWebView2.Environment, tab, TabManager);
				// 	// _extensionPopups.Add(popupWindow);
				// 	// popupWindow.Show();
				// }
				// else if (e.Uri.Contains("AS_POPUP:"))
				// {
				// 	
				// 	// var popupWindow = new ExtensionPopupWindow(e.Uri.Split("AS_POPUP:")[1], tab.TabCore.CoreWebView2.Environment, tab, TabManager);
				// 	// _extensionPopups.Add(popupWindow);
				// 	// popupWindow.Show();
				// }
				//{
					TabManager.SwapActiveTabTo(TabManager.AddTab(e.Uri));
					e.Handled = true;
				//}
			};
		};
		TabManager.TabCreated += tab =>
		{
			var tabCard = new TabCard(tab);

			//TabHolder.Children.Add(tab.TabCore);

			tabCard.DragPositionChanged += async (s, e) => await OnTabCardDragChanged(s, e);

			tab.TitleChanged += () => { tabCard.TitleLabel.Content = tab.Title; };

			tab.ImageChanged += () => { tabCard.TabIcon.Child = tab.Icon; };
			
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

			if (_instanceData.PinInfo.GetAllTabInfos()
			    .Any(t => t.Value.Url == (tab?.TabCore?.Source?.ToString() ?? "__NULL__")))
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.Pin };
				StackPin.BorderBrush = Brushes.White;
				LabelPin.Content = "Unpin Tab";
			}
			else
			{
				ButtonPin.Content = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
				StackPin.BorderBrush = new SolidColorBrush(MainColor);
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
				Margin = new Thickness(5, 0, 5, 0),
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
	
	#region Extension Popup stuff
	public class ExtensionPopupWindow : Window
	{
		public readonly WebView2CompositionControl _webView2;
		private readonly WebsiteTab _parentTab;
		private readonly TabManager _tabManager;
		// private static int _popupCounter = 1000000000; //TODO: probably not an issue, but fix this later

		public ExtensionPopupWindow(string popupUrl, CoreWebView2Environment environment, WebsiteTab parentTab,
			TabManager tabManager)
		{
			_parentTab = parentTab;
			_tabManager = tabManager;
			Width = 400;
			Height = 300;
			WindowStyle = WindowStyle.ToolWindow;
			_webView2 = new WebView2CompositionControl();
			Content = _webView2;

			Closed += (_, _) =>
			{
				_webView2?.Dispose();
			};
			
			SourceInitialized  += async (_, _) =>
			{
				await _webView2.EnsureCoreWebView2Async(environment);
				// // await _webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
				// // 	"""
				// // 	(function() {
				// // 	  // 1) Keep a reference to the real one
				// // 	  const realGetCurrent = chrome.windows.getCurrent.bind(chrome.windows);
				// // 	
				// // 	  // 2) Override it
				// // 	  chrome.windows.getCurrent = function(getInfoOrCallback, maybeCallback) {
				// // 	    // Signature allows two styles: getCurrent(callback)  OR  getCurrent({populate:bool}, callback)
				// // 	    let getInfo = {};
				// // 	    let callback;
				// // 	
				// // 	    if (typeof getInfoOrCallback === "function") {
				// // 	      callback = getInfoOrCallback;
				// // 	    } else {
				// // 	      getInfo = getInfoOrCallback || {};
				// // 	      callback = maybeCallback;
				// // 	    }
				// // 	
				// // 	    // 3) Call the real one to get a Window object (ChromeWindowModel)
				// // 	    realGetCurrent(getInfo, (windowObj) => {
				// // 	      // Push it up to C#
				// // 	      window.chrome.webview.postMessage({
				// // 	        type: "windowsGetCurrent",
				// // 	        payload: windowObj
				// // 	      });
				// // 	
				// // 	      // 4) Wait for C# to send back a "windowsGetCurrentResponse"
				// // 	      function onHostMessage(ev) {
				// // 	        let msg = ev.data;
				// // 	        if (msg.type === "windowsGetCurrentResponse") {
				// // 	          window.chrome.webview.removeEventListener("message", onHostMessage);
				// // 	          // Invoke the original callback with the patched window object
				// // 	          callback(msg.payload);
				// // 	        }
				// // 	      }
				// // 	      window.chrome.webview.addEventListener("message", onHostMessage);
				// // 	    });
				// // 	  };
				// // 	})();
				// // 	""");
				// await _webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
				// 	"(function() {\n  // Keep a reference to the real chrome.tabs.query\n  const realQuery = chrome.tabs.query.bind(chrome.tabs);\n\n  chrome.tabs.query = function(queryInfo, maybeCallback) {\n    // Determine if the caller passed a callback or expects a Promise\n    const isCallbackStyle = typeof maybeCallback === \"function\";\n    const userCallback = isCallbackStyle ? maybeCallback : null;\n\n    // Helper to do the real query + postMessage → wait for response\n    function doQueryWithHost(queryInfo, resolver) {\n      // 1) Call the real (Chromium) tabs.query(queryInfo, ...)\n      realQuery(queryInfo, (tabsArray) => {\n        // 2) Send raw list + queryInfo to C# so it can filter 'active' (etc.)\n        window.chrome.webview.postMessage({\n          type: \"tabsQuery\",\n          payload: {\n            tabs: tabsArray,\n            queryInfo: queryInfo\n          }\n        });\n\n        // 3) Listen for a single \"tabsQueryResponse\" from C#\n        function handleHostMessage(ev) {\n          const msg = ev.data;\n          if (msg.type === \"tabsQueryResponse\") {\n            window.chrome.webview.removeEventListener(\"message\", handleHostMessage);\n            // Now we have the filtered/fixed array\n            resolver(msg.payload);\n          }\n        }\n        window.chrome.webview.addEventListener(\"message\", handleHostMessage);\n      });\n    }\n\n    if (isCallbackStyle) {\n      // Old callback style: we need to pass queryInfo and a callback\n      doQueryWithHost(queryInfo, (fixedTabs) => {\n        // just call the original callback with the fixed array\n        userCallback(fixedTabs);\n      });\n      return; // no return value in callback style\n    } else {\n      // Promise style: return a Promise that resolves with the fixed array\n      return new Promise((resolve) => {\n        doQueryWithHost(queryInfo, resolve);\n      });\n    }\n  };\n})();\n");
				// _webView2.CoreWebView2.NewWindowRequested += (_, e) =>
				// {
				// 	_tabManager.SwapActiveTabTo(_tabManager.AddTab(e.Uri));
				// 	e.Handled = true;
				// };
				// _webView2.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
				//
				_webView2.Source = new Uri(popupUrl/*+"#586966453"*/);
			};
		}
		
		// private async void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
		// {
		// 	var json = e.WebMessageAsJson;
		//
		// 	using var doc = JsonDocument.Parse(json);
		// 	var root = doc.RootElement;
		// 	if (root.GetProperty("type").GetString() == "tabsQuery")
		// 	{
		// 		var payload = root.GetProperty("payload");
		// 		var tabsJson = payload.GetProperty("tabs").GetRawText();
		// 		var allTabs = JsonSerializer.Deserialize<List<TabModel>>(tabsJson);
		// 		
		// 		var activeUrl = _tabManager.GetTab(_tabManager.ActiveTabId)!.TabCore.Source.ToString();
		// 		foreach (var t in allTabs!)
		// 		{
		// 			t.Active = t.Url == activeUrl;
		// 		}
		//
		// 		var queryInfoJson = payload.GetProperty("queryInfo").GetRawText();
		// 		var queryInfo = JsonSerializer.Deserialize<QueryInfoModel>(queryInfoJson);
		// 		if (queryInfo?.active??false)
		// 		{
		// 			allTabs = allTabs.Where(t => t.Active).ToList();
		// 		}
		// 		
		// 		
		// 		// Send it back:
		// 		var replyObj = new
		// 		{
		// 			type = "tabsQueryResponse",
		// 			payload = allTabs
		// 		};
		// 		string replyJson = JsonSerializer.Serialize(replyObj);
		// 		_webView2.CoreWebView2.PostWebMessageAsJson(replyJson);
		// 	}
		// }
	}
	
	public class QueryInfoModel
	{
		public bool active { get; set; }
		public bool lastFocusedWindow { get; set; } //TODO
	}
	
	public class TabModel
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }            // Tab ID (null if not yet created)

    [JsonPropertyName("index")]
    public int Index { get; set; }          // Position within the window's tab strip

    [JsonPropertyName("windowId")]
    public int WindowId { get; set; }       // ID of the window this tab belongs to

    [JsonPropertyName("openerTabId")]
    public int? OpenerTabId { get; set; }   // If this tab was opened via window.open or browser.tabs.create from another tab

    [JsonPropertyName("highlighted")]
    public bool Highlighted { get; set; }   // True if the tab is highlighted

    [JsonPropertyName("active")]
    public bool Active { get; set; }        // True if this is the active (focused) tab in its window

    [JsonPropertyName("pinned")]
    public bool Pinned { get; set; }        // True if the tab is pinned

    [JsonPropertyName("audible")]
    public bool? Audible { get; set; }      // True if tab is producing sound (optional)

    [JsonPropertyName("discarded")]
    public bool Discarded { get; set; }     // True if the tab was automatically discarded

    [JsonPropertyName("autoDiscardable")]
    public bool AutoDiscardable { get; set; } // True if the tab can be discarded automatically

    [JsonPropertyName("mutedInfo")]
    public MutedInfoModel MutedInfo { get; set; }  // Information about mute state

    [JsonPropertyName("status")]
    public string Status { get; set; }      // "loading" or "complete"

    [JsonPropertyName("title")]
    public string Title { get; set; }       // Page title of the tab

    [JsonPropertyName("url")]
    public string Url { get; set; }         // URL of the tab

    [JsonPropertyName("favIconUrl")]
    public string FavIconUrl { get; set; }  // URL of the tab's favicon (if any)

    [JsonPropertyName("incognito")]
    public bool Incognito { get; set; }     // True if the tab is in incognito mode

    [JsonPropertyName("width")]
    public int? Width { get; set; }         // Width (px) of the rendered tab (optional)

    [JsonPropertyName("height")]
    public int? Height { get; set; }        // Height (px) of the rendered tab (optional)

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; }   // A unique session ID for the tab (if session restore is on)

    [JsonPropertyName("discardReason")]
    public string DiscardReason { get; set; } // Reason why the tab was discarded (optional)

    [JsonPropertyName("mutedReason")]
    public string MutedReason { get; set; }   // Why the tab was muted (e.g. "user", "capture", "extension") (optional)

    [JsonPropertyName("audibleState")]
    public string AudibleState { get; set; } // E.g. "audible", "silent", or "unknown" (in newer Chrome versions)

    // If an extension or content script has inserted its own “highlighted” field or other custom properties,
    // you may need to add those here, but for vanilla Chrome/Edge these cover the official spec.
}

	// Model for the “mutedInfo” sub‐object:
	public class MutedInfoModel
	{
	    [JsonPropertyName("muted")]
	    public bool Muted { get; set; }           // true if the tab is currently muted

	    [JsonPropertyName("extensionId")]
	    public string ExtensionId { get; set; }   // Which extension muted it (optional)

	    [JsonPropertyName("reason")]
	    public string Reason { get; set; }        // e.g. "user", "capture", "extension", or "system" (optional)

	    [JsonPropertyName("disabled")]
	    public bool Disabled { get; set; }        // true if the tab is forced unmuted even if “muted” is true
	}
	#endregion
}