using FoxyBrowser716_WinUI.DataManagement;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using FoxyBrowser716_WinUI.DataObjects.Complex;
using Microsoft.UI.Windowing;
using WinUIEx;

// using CommunityToolkit.WinUI.Helpers;
//

namespace FoxyBrowser716_WinUI.Controls.MainWindow;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public TabManager TabManager { get; private set; }
    public Instance Instance { get; private set; }
    
    private MainWindow()
    {
        InitializeComponent();
        
        // initial is needed to allow clicks for other buttons
        SetTitleBar(TopBar.DragZone); 
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            ExtendsContentIntoTitleBar = true;
            p.SetBorderAndTitleBar(true, false);
        }
        else
            throw new Exception("AppWindowPresenterKind is not OverlappedPresenter, cannot setup the window properly!");
        
        TopBar.DragZone.PointerEntered += (_, _) =>
        {
            // to fix a bug with this becoming unset for whatever reason
            SetTitleBar(TopBar.DragZone);
        };
        
        ApplyTheme();
    }

    public static async Task<MainWindow> Create(Instance instance)
    {
        var win = new MainWindow();
        await win.Initialize(instance);
        return win;
    }

    private async Task Initialize(Instance instance)
    {
        Instance = instance;
        TabManager = await TabManager.Create(Instance);
        
        TabManager.ActiveTabChanged += TabManagerOnActiveTabChanged;
        TabManager.TabAdded += TabManagerOnTabAdded;
        TabManager.TabRemoved += TabManagerOnTabRemoved;
        
        await LeftBar.Initialize(TabManager);

    }

    private void TabManagerOnTabRemoved(WebviewTab tab)
    {
        //TODO
    }

    private async void TabManagerOnTabAdded(WebviewTab tab)
    {
        TabHolder.Children.Add(tab.Core);

        tab.Info.PropertyChanged += (_, _) =>
        {
            if (TabManager.ActiveTabId == tab.Id)
            {
                RefreshCurrentTabUi(tab);
            }
        };
        await tab.InitializeTask;
        tab.Core.CoreWebView2.HistoryChanged += (_, _) =>
        {
            if (TabManager.ActiveTabId == tab.Id)
            {
                RefreshCurrentTabUi(tab);
            }
        };
    }

    private void RefreshCurrentTabUi(WebviewTab? tab, int? browserWindowId = null)
    {
        if (tab is not null)
        {
            TopBar.UpdateSearchBar(true, tab.Core.CanGoBack, tab.Core.CanGoForward, tab.Info.Url);
        }
        else if (browserWindowId is not null)
        {
            TopBar.UpdateSearchBar(false, false, false, string.Empty);
            HomePage.Visibility = browserWindowId == -1 ? Visibility.Visible : Visibility.Collapsed;
            SettingsPage.Visibility = browserWindowId == -2 ? Visibility.Visible : Visibility.Collapsed;
        }
        else
            throw new Exception("when refresh ui is called, tab or browserWindowId must be provided.");
    }

    private void TabManagerOnActiveTabChanged((int oldId, int newId) pair)
    {
        if (pair.newId < 0)
            RefreshCurrentTabUi(null, pair.newId);
        else if (TabManager.TryGetTab(pair.newId, out var tab))
            RefreshCurrentTabUi(tab);
        else
            throw new Exception($"Could not retrieve tab with id '{pair.newId}'");
    }

    private Theme _currentTheme = DefaultThemes.DarkMode;
    internal Theme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            _currentTheme = value;
            ApplyTheme();
        }
    }

    private void ApplyTheme()
    {
        //TODO: remove apply theme from other constructors
        TopBar.CurrentTheme = CurrentTheme;
        LeftBar.CurrentTheme = CurrentTheme;
        
        Root.Background = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);
        BorderGrid.BorderBrush = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);
        TabHolder.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColor);
    }
    
    
    

    #region Window Events
    private void TopBar_OnMinimizeClicked()
    {
        // can't click this in fullscreen, and will cause an error if this runs while in fullscreen
        if (InFullscreen) return;
        
        this.Minimize();
    }

    private void TopBar_OnMaximizeClicked()
    {
        // can't click this in fullscreen, and will cause an error if this runs while in fullscreen
        if (InFullscreen) return;
        
        if (WindowState == WindowState.Maximized)
            this.Restore();
        else
            this.Maximize();
    }

    private void TopBar_OnCloseClicked()
    {
        this.Close();
    }
    
    private void TopBar_OnBorderlessToggled()
    {
        AppWindow.SetPresenter(TopBar.IsBorderless
            ? AppWindowPresenterKind.FullScreen
            : AppWindowPresenterKind.Default);
    }
    
    public enum BrowserWindowState
    {
        Minimized,
        Normal,
        Maximized,
        Borderless
    }

    public BrowserWindowState StateFromWindow()
    {
        if (InFullscreen) return BrowserWindowState.Borderless;

        return WindowState switch
        {
            WindowState.Minimized => BrowserWindowState.Minimized,
            WindowState.Normal => BrowserWindowState.Normal,
            WindowState.Maximized => BrowserWindowState.Maximized,
        };
    }

    public void ApplyWindowState(BrowserWindowState windowState)
    {
        TopBar.ToggleBorderless(windowState == BrowserWindowState.Borderless);
        
        AppWindow.SetPresenter(TopBar.IsBorderless
            ? AppWindowPresenterKind.FullScreen
            : AppWindowPresenterKind.Default);
        
        switch (windowState)
        {
            case BrowserWindowState.Minimized:
                this.Minimize();
                break;
            case BrowserWindowState.Normal:
                this.Restore();
                break;
            case BrowserWindowState.Maximized:
                this.Maximize();
                break;
            case BrowserWindowState.Borderless:
                //already there with the fullscreen presenter
                break;
        }
    }
    
    internal bool InFullscreen => AppWindow.Presenter is FullScreenPresenter;
    #endregion

    private async void TopBar_OnSearchClicked(string searchText)
    {
        if (TabManager.ActiveTabId >= 0 && TabManager.TryGetTab(TabManager.ActiveTabId, out var tab))
        {
            await tab!.NavigateOrSearch(searchText);
        }
        else if (TabManager.ActiveTabId < 0)
        {
            TabManager.SwapActiveTabTo(TabManager.AddTab(searchText));
        }
    }
}