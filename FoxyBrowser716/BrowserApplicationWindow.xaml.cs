using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
	//private SettingsPage _settingsPage;
	
	private List<ExtensionPopupWindow> _extensionPopups = [];

	private System.Windows.Rect _originalRectangle = new(0,0,350,200);

	internal readonly TabManager TabManager;

	private InstanceManager _instanceData;

	internal Task InitTask;

	private WindowState _prevWindowState = WindowState.Normal;
	private WindowState _curWindowState = WindowState.Normal;
	
	private IntPtr _hwnd;

	private bool ignoreNextStateChange;
	
	public BrowserApplicationWindow(InstanceManager instanceData)
	{
		InitializeComponent();
		
		SetupAnimationsAndColors();
		
		SourceInitialized += (_, _) =>
		{
			_hwnd = new WindowInteropHelper(this).Handle;
		};

		Loaded += (_, _) =>
		{
			_originalRectangle = new Rect(Left, Top, Width, Height);
		};
		
		StateChanged += (_, _) =>
		{
			_prevWindowState = _curWindowState;
			_curWindowState = WindowState;
			
			if (_curWindowState == WindowState.Maximized)
			{
				ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.WindowRestore };
				ButtonFullscreen.Content = BorderlessFullscreen 
					? new MaterialIcon { Kind = MaterialIconKind.ArrowCollapse } 
					: new MaterialIcon { Kind = MaterialIconKind.ArrowExpand };
			}
			else
			{
				ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.Maximize };
				ButtonFullscreen.Content = new MaterialIcon { Kind = MaterialIconKind.ArrowExpand };
			}
			
			if (_curWindowState == WindowState.Normal)
			{
				if (!ignoreNextStateChange)
				{
					#region WindowResizingStuff
					var oldRect = new RECT {
						left   = (int)_originalRectangle.X, top    = (int)_originalRectangle.Y,
						right  = (int)(_originalRectangle.X + _originalRectangle.Width),
						bottom = (int)(_originalRectangle.Y + _originalRectangle.Height)
					};
					var oldMon = MonitorFromRect(ref oldRect, MONITOR_DEFAULTTONEAREST);
					var miOld = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
					GetMonitorInfo(oldMon, ref miOld);
					var waOld = miOld.rcWork;
					var oldWorkArea = new Rect(waOld.left, waOld.top,
						waOld.right - waOld.left, waOld.bottom - waOld.top);
					
					var relX = (_originalRectangle.X - oldWorkArea.Left) / oldWorkArea.Width;
					var relY = (_originalRectangle.Y - oldWorkArea.Top) / oldWorkArea.Height;

					var hMonNew = MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST);
					var miNew = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
					GetMonitorInfo(hMonNew, ref miNew);
					var waNew = miNew.rcWork;

					Left   = waNew.left + relX * (waNew.right - waNew.left);
					Top    = waNew.top  + relY * (waNew.bottom - waNew.top);
					Width  = _originalRectangle.Width;
					Height = _originalRectangle.Height;
					#endregion
				}
			}
			
			Background = new SolidColorBrush(WindowState == WindowState.Maximized ? HighlightColor : Colors.Transparent);
			ignoreNextStateChange = false;
		};
		
		ForwardButton.Visibility = Visibility.Collapsed;
		BackButton.Visibility = Visibility.Collapsed;
		RefreshButton.Visibility = Visibility.Collapsed;
		
		ForwardButton.Click += (_, _) =>
		{
			if (TabManager?.GetTab(TabManager.ActiveTabId) is { } tab)
			{
				tab.TabCore.GoForward();
			}
		};
		BackButton.Click += (_, _) =>
		{
			if (TabManager?.GetTab(TabManager.ActiveTabId) is { } tab)
			{
				tab.TabCore.GoBack();
			}
		};
		RefreshButton.Click += (_, _) =>
		{
			if (TabManager?.GetTab(TabManager.ActiveTabId) is { } tab)
			{
				tab.TabCore.Reload();
			}
		};
		
		ButtonClose.Click += (s, e) => { Close(); };
		ButtonMinimize.Click += (s, e) => { WindowState = WindowState.Minimized; };
		
		ButtonMaximize.Click += MaximizeRestore_Click;
		ButtonFullscreen.Click += FullscreenRestore_Click;
		
		_instanceData = instanceData;
		TabManager = new TabManager(_instanceData);
		InitTask = Initialize();

		BlurredBackground.Effect = new BlurEffect { Radius = 25 };
		BlurredBackground.Opacity = 1;
		var visualBrush = new VisualBrush(TabHolder)
		{
			Stretch = Stretch.UniformToFill,
			TileMode = TileMode.None,
			Transform = new ScaleTransform(1.3, 1.3, 0.5, 0.5)
		};
		BlurredBackground.Background = visualBrush;

		SearchButton.Click += async (s, e) => await Search_Click(s, e);

		//StateChanged += Window_StateChanged;
		//Window_StateChanged(null, EventArgs.Empty);
		
		ButtonMaximize.Content = new MaterialIcon { Kind = MaterialIconKind.Maximize };
		ButtonFullscreen.Content = new MaterialIcon { Kind = MaterialIconKind.ArrowExpand };
		
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

		SearchBox.KeyDown += async (_, e) =>
		{
			if (e.Key == Key.Enter)
			{
				await Search_Click(null, EventArgs.Empty);
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
	
	private async Task Search_Click(object? s, EventArgs e)
	{
		if (TabManager.GetTab(TabManager.ActiveTabId) is { } tab)
		{
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
		else
		{
			TabManager.SwapActiveTabTo(TabManager.AddTab(Uri.EscapeDataString(SearchBox.Text)));
		}
	}
	
	private static readonly Color _transparentBack = Color.FromArgb(225, 48, 50, 58);
	private static readonly Color _transparentAccent = Color.FromArgb(255, AccentColor.R, AccentColor.G, AccentColor.B);
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
			         ButtonFullscreen, ButtonMaximize, ButtonMinimize, ButtonMenu,
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
	public bool BorderlessFullscreen { get; set; } = false;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
    }

    private IntPtr WndProc(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        const int WM_GETMINMAXINFO      = 0x0024;
        const int WM_WINDOWPOSCHANGING  = 0x0046;

        if (msg == WM_WINDOWPOSCHANGING)
        {
            HandleWindowPosChanging(hwnd, lParam);
            // don’t mark handled = true here, let default still apply
        }
        else if (msg == WM_GETMINMAXINFO)
        {
            WmGetMinMaxInfo(hwnd, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void HandleWindowPosChanging(IntPtr hwnd, IntPtr lParam)
    {
        // only care if we’re “maximized”
        if (WindowState != WindowState.Maximized) return;

        // pull in the proposed new pos/size
        var wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);

        // build a RECT of that
        var proposed = new RECT
        {
            left   = wp.x,
            top    = wp.y,
            right  = wp.x + wp.cx,
            bottom = wp.y + wp.cy
        };

        // figure out which monitor THAT rect is on
        IntPtr hMon = MonitorFromRect(ref proposed, MONITOR_DEFAULTTONEAREST);
        if (hMon == IntPtr.Zero) return;

        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(hMon, ref mi);

        RECT rcMon  = mi.rcMonitor;  // full
        RECT rcWork = mi.rcWork;     // work area

        if (BorderlessFullscreen)
        {
            wp.x  = rcMon.left;
            wp.y  = rcMon.top;
            wp.cx = rcMon.right  - rcMon.left;
            wp.cy = rcMon.bottom - rcMon.top;
        }
        else
        {
            wp.x  = rcWork.left;
            wp.y  = rcWork.top;
            wp.cx = rcWork.right  - rcWork.left;
            wp.cy = rcWork.bottom - rcWork.top;
        }

        // write it back
        Marshal.StructureToPtr(wp, lParam, true);
    }

    private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
    {
        // fallback when WM_WINDOWPOSCHANGING isn’t enough
        var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
        GetWindowRect(hwnd, out RECT winRect);

        IntPtr hMon = MonitorFromRect(ref winRect, MONITOR_DEFAULTTONEAREST);
        if (hMon != IntPtr.Zero)
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            GetMonitorInfo(hMon, ref mi);

            RECT rcMon  = mi.rcMonitor;
            RECT rcWork = mi.rcWork;

            if (BorderlessFullscreen)
            {
                mmi.ptMaxPosition.x = 0;
                mmi.ptMaxPosition.y = 0;
                mmi.ptMaxSize.x     = rcMon.right  - rcMon.left;
                mmi.ptMaxSize.y     = rcMon.bottom - rcMon.top;
            }
            else
            {
                mmi.ptMaxPosition.x = rcWork.left - rcMon.left;
                mmi.ptMaxPosition.y = rcWork.top  - rcMon.top;
                mmi.ptMaxSize.x     = rcWork.right  - rcWork.left;
                mmi.ptMaxSize.y     = rcWork.bottom - rcWork.top;
            }
        }

        Marshal.StructureToPtr(mmi, lParam, true);
    }

    #region Interop

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    
    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(
        IntPtr hMonitor,
        ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromRect(
        [In] ref RECT lprc,
        uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(
        IntPtr hWnd,
        out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x, y, cx, cy;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int left, top, right, bottom;
    }

    #endregion
	
	private bool _resizeInProcess;
	private bool wasMaximizedBeforeFullscreen;

	private bool _potentialDrag;
	private Point _mouseDownPos;

	private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		_potentialDrag = false;
	}
	
	private void TitleBar_MouseMove(object sender, MouseEventArgs e)
	{
		if (!_potentialDrag || e.LeftButton != MouseButtonState.Pressed)
			return;

		var currentPos = e.GetPosition(this);
		var dx = Math.Abs(currentPos.X - _mouseDownPos.X);
		var dy = Math.Abs(currentPos.Y - _mouseDownPos.Y);

		if (dx >= SystemParameters.MinimumHorizontalDragDistance * 3 ||
		    dy >= SystemParameters.MinimumVerticalDragDistance * 3)
		{
			_potentialDrag = false;
			#region WindowResizeStuff
			var click = e.GetPosition(this);
			var screenClick = PointToScreen(click);

			var hMon = MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST);
			var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
			GetMonitorInfo(hMon, ref mi);
			var wa = mi.rcWork;
			double maxW = wa.right - wa.left;
			double maxH = wa.bottom - wa.top;

			var scaleX = _originalRectangle.Width  / maxW;
			var scaleY = _originalRectangle.Height / maxH;

			var offsetX = click.X * scaleX;
			var offsetY = click.Y * scaleY;

			ignoreNextStateChange = true;
			WindowState = WindowState.Normal;

			Width  = _originalRectangle.Width;
			Height = _originalRectangle.Height;
			Left   = screenClick.X - offsetX;
			Top    = screenClick.Y - offsetY;
			#endregion
			DragMove();
		}
	}
	
	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount == 2)
		{
			MaximizeRestore_Click(null, EventArgs.Empty);
			return;
		}
		
		if (WindowState != WindowState.Normal)
		{
			_potentialDrag = true;
		}
		else
		{
			DragMove();
		}
	}
	
	private void FullscreenRestore_Click(object? sender, EventArgs e)
	{
		if (BorderlessFullscreen)
		{
			BorderlessFullscreen = false;
			if (!wasMaximizedBeforeFullscreen)
				WindowState = WindowState.Normal;
			else
			{
				WindowState = WindowState.Normal;
				WindowState = WindowState.Maximized;
			}
		}
		else
		{
			wasMaximizedBeforeFullscreen = WindowState == WindowState.Maximized;
			if (!wasMaximizedBeforeFullscreen)
				_originalRectangle = new System.Windows.Rect(Left, Top, Width, Height);
			else
				WindowState = WindowState.Normal;
			BorderlessFullscreen = true;
			WindowState = WindowState.Maximized;
		}
	}
	
	private void MaximizeRestore_Click(object? sender, EventArgs e)
	{
		if (WindowState == WindowState.Maximized)
		{
			BorderlessFullscreen = false;
			WindowState = WindowState.Normal;
		}
		else
		{
			_originalRectangle = new System.Windows.Rect(Left, Top, Width, Height);
			WindowState = WindowState.Maximized;
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

	private void TabHolder_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		var geometry = new RectangleGeometry(
			new Rect(0, 0, TabHolder.ActualWidth, TabHolder.ActualHeight),
			8, 8); // RadiusX and RadiusY set to 8 (inner corner radius)
		TabHolder.Clip = geometry;
	}
	
	private void Blur_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		var geometry = new RectangleGeometry(
			new Rect(0, 0, BlurredBackground.ActualWidth, BlurredBackground.ActualHeight),
			8, 8); // RadiusX and RadiusY set to 8 (inner corner radius)
		BlurredBackground.Clip = geometry;
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
		Background = new SolidColorBrush(_transparentBack),
		BorderBrush = new SolidColorBrush(_transparentAccent),
		BorderThickness = new Thickness(2), 
		HasDropShadow = true, 
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
		throw new NotImplementedException();
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
						await i.CreateWindow();
					else
					{
						await i.CreateWindow(null, new System.Windows.Rect(Left, Top, Width, Height),
							InstanceManager.StateFromWindow(this));
						Close();
					}
				});
		instanceOptions.Add("Manage Instances", () => throw new NotImplementedException());
		ApplyMenuItems(_menu, instanceOptions);
		_menu.PlacementTarget = ButtonMenu;
		_menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
		_menu.IsOpen = true;
	}

	private void OpenBookmarks()
	{
		ApplyMenuItems(_menu, _instanceData.BookmarkInfo.GetAllTabInfos().Values
			.ToDictionary<TabInfo, string, Action>(k => k.Title, k => () => 
				TabManager.SwapActiveTabTo(TabManager.AddTab(k.Url))
		));
		_menu.PlacementTarget = ButtonMenu;
		_menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
		_menu.IsOpen = true;
	}

	private void OpenHistory()
	{
		// var tier = RenderCapability.Tier >> 16;
		// switch (tier)
		// {
		// 	case 0:
		// 		Console.WriteLine("No hardware acceleration (rendering tier 0).");
		// 		break;
		// 	case 1:
		// 		Console.WriteLine("Partial hardware acceleration (rendering tier 1).");
		// 		break;
		// 	case 2:
		// 		Console.WriteLine("Full hardware acceleration (rendering tier 2).");
		// 		break;
		// }
		throw new NotImplementedException();
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
		ApplyMenuItems(_menu, extensions
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
		_menu.PlacementTarget = ButtonMenu;
		_menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
		_menu.IsOpen = true;
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
		var tabCardWindow = new TabMoveWindowCard(tab, _instanceData)
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
			//_settingsPage.Visibility = newActiveTab == -2 ? Visibility.Visible : Visibility.Collapsed;
			
			foreach (var card in Tabs.Children.OfType<TabCard>())
			{
				if (card.Key == oldActiveTab)
					card.ToggleActiveTo(false);
				else if (card.Key == newActiveTab)
					card.ToggleActiveTo(true);
			}
			
			var tab = TabManager.GetTab(newActiveTab);
			SearchBox.Text = tab?.TabCore?.Source?.ToString() ?? "";
				
			ForwardButton.Visibility = tab?.TabCore?.CanGoForward?? false ? Visibility.Visible : Visibility.Collapsed;
			BackButton.Visibility = tab?.TabCore?.CanGoBack?? false ? Visibility.Visible : Visibility.Collapsed;
			
			if (newActiveTab > 0)
			{
				RefreshButton.Visibility = Visibility.Visible;
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


		//_settingsPage = PrimarySettingsPage.GeneratePage(null); //TODO
		//TabHolder.Children.Add(_settingsPage);

		//_settingsPage.Loaded += (_, _) =>
			//_settingsPage.Visibility = TabManager.ActiveTabId == -2 ? Visibility.Visible : Visibility.Collapsed;
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

			if (tab.TabCore.CoreWebView2 is null)
				tab.TabCore.CoreWebView2InitializationCompleted += (_, _) =>
					CoreWebView2EventHandlersSetup();
			else
				CoreWebView2EventHandlersSetup();
			

			void CoreWebView2EventHandlersSetup()
			{
				tab.TabCore.CoreWebView2.HistoryChanged += (_, _) =>
				{
					ForwardButton.Visibility = tab.TabCore.CanGoForward ? Visibility.Visible : Visibility.Collapsed;
					BackButton.Visibility = tab.TabCore.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
				};
			}
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
				_webView2.Source = new Uri(popupUrl/*+"#586966453"*/);
			};
		}
	}
	
	#endregion
}