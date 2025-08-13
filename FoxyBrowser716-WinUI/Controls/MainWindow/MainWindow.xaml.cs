using System.Diagnostics;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using FoxyBrowser716_WinUI.Controls.Generic;
using FoxyBrowser716_WinUI.DataManagement;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using FoxyBrowser716_WinUI.DataObjects.Complex;
using Material.Icons;
using Material.Icons.WinUI3;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIEx;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Graphics.Dwm;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Web.WebView2.Core;


// using CommunityToolkit.WinUI.Helpers;
//

namespace FoxyBrowser716_WinUI.Controls.MainWindow;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public TabManager TabManager { get; private set; }
    public Instance Instance { get; private set; }

    public Action<InfoGetter.SearchEngine> SearchEngineChangeRequested;
    
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
        
        TopBar.UpdateMaximizeRestore(WindowState);
        ApplyTheme();
    }

    public static async Task<MainWindow> Create(Instance instance)
    {
        var win = new MainWindow();
        await win.Initialize(instance);
        return win;
    }

    private void HandleCacheChanged()
    {
        TopBar.UpdateSearchEngineIcon(Instance.Cache.CurrentSearchEngine);
        TopBar.SetSidebarLockedState(Instance.Cache.LeftBarLocked);
        LeftBar.SetLockedState(Instance.Cache.LeftBarLocked);
    }

    private async Task Initialize(Instance instance)
    {
        Instance = instance;
        TabManager = await TabManager.Create(Instance);
        
        await Task.WhenAll(LeftBar.Initialize(TabManager), HomePage.Initialize(TabManager, Instance));
        
        // link events from tab manager
        TabManager.ActiveTabChanged += TabManagerOnActiveTabChanged;
        TabManager.TabAdded += TabManagerOnTabAdded;
        TabManager.TabRemoved += TabManagerOnTabRemoved;
        
        // link events from the instance
        instance.Cache.PropertyChanged += (_, _) => HandleCacheChanged();
        
        // link events from home page
        HomePage.ToggleEditMode += HomePageOnToggleEditMode;
        
        TabManager.TryGetTab(TabManager.ActiveTabId, out var tab);
        RefreshCurrentTabUi(tab, TabManager.ActiveTabId < 0 ? TabManager.ActiveTabId : null);
        
        await ExtensionPopupWebview.EnsureCoreWebView2Async(TabManager.WebsiteEnvironment);
        
        // refresh all data
        HandleCacheChanged();
    }

    private void HomePageOnToggleEditMode(bool inEdit)
    {
        LeftBar.ToggleEditMode(inEdit, HomePage);
        TopBar.ToggleEditMode(inEdit);
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


    private record PopupSize(double width, double height);
    private async void RefreshCurrentTabUi(WebviewTab? tab, int? browserWindowId = null)
    {
        if (tab is not null)
        {
            TopBar.UpdateSearchBar(true, tab.Core.CanGoBack, tab.Core.CanGoForward, tab.Info.Url);

            
            ExtensionPopupWebview.CoreWebView2.NewWindowRequested +=
                (_, args) =>
                {
                    TabManager.SwapActiveTabTo(TabManager.AddTab(args.Uri));
                    args.Handled = true;
                };
            ExtensionPopupWebview.NavigationCompleted += async (_, _) =>
            {
                //TODO: not working as expected (crazy high width + height)
                /*var result = await ExtensionPopupWebview.ExecuteScriptAsync(
                    """
                    (function(){
                        return {
                            width: document.documentElement.scrollWidth,
                            height: document.documentElement.scrollHeight
                        };
                    })();
                    """
                    );*/
                //var size = JsonSerializer.Deserialize<PopupSize>(result);
                ExtensionPopupWebview.Width = 350;//size.width;
                ExtensionPopupWebview.Height = 700; //size.height;
            };
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
        
        //TODO test and fine tune
        
        // apply fading:
        // var fadeOut = AnimationBuilder.Create();
        var fadeIn = AnimationBuilder.Create();
        
        // already collapsed?
        // if (pair.oldId == -1)
        // {
        //     Canvas.SetZIndex(HomePage, 0);
        //     fadeOut.Opacity(0, null, null, TimeSpan.FromSeconds(1), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml);
        //     _ = fadeOut.StartAsync(HomePage);
        // }
        // else if (pair.oldId == -2)
        // {
        //     Canvas.SetZIndex(SettingsPage, 0);
        //     fadeOut.Opacity(0, null, null, TimeSpan.FromSeconds(1), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml);
        //     _ = fadeOut.StartAsync(SettingsPage);
        // } 
        // else if (TabManager.TryGetTab(pair.oldId, out var oldTab))
        // {
        //     Canvas.SetZIndex(oldTab!.Core, 0);
        //     fadeOut.Opacity(0, null, null, TimeSpan.FromSeconds(1), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml);
        //     _ = fadeOut.StartAsync(oldTab!.Core);
        // }
        
        if (pair.newId == -1)
        {
            Canvas.SetZIndex(HomePage, 1);
            fadeIn.Opacity(1, 0, null, TimeSpan.FromSeconds(1), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml);
            _ = fadeIn.StartAsync(HomePage);
        }
        else if (pair.newId == -2)
        {
            Canvas.SetZIndex(SettingsPage, 1);
            fadeIn.Opacity(1, 0, null, TimeSpan.FromSeconds(1), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml);
            _ = fadeIn.StartAsync(SettingsPage);
        } 
        else if (TabManager.TryGetTab(pair.newId, out var newTab))
        {
            Canvas.SetZIndex(newTab!.Core, 1);
            fadeIn.Opacity(1, 0, null, TimeSpan.FromSeconds(1), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml);
            _ = fadeIn.StartAsync(newTab!.Core);
        }
    }

    internal Theme CurrentTheme
    {
        get;
        set
        {
            field = value;
            ApplyTheme();
        }
    } = DefaultThemes.VastSea;

    private void ApplyTheme()
    {
        //TODO: remove apply theme from other constructors
        TopBar.CurrentTheme = CurrentTheme;
        LeftBar.CurrentTheme = CurrentTheme;
        ContextMenuPopup.CurrentTheme = CurrentTheme;
        HomePage.CurrentTheme = CurrentTheme;
        
        PopupRoot.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        PopupRoot.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);
        
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

    private void TopBar_OnMenuClicked()
    {
        if (HomePage.InEditMode)
        {
            return;
        }
        
        _contextmenuUsedForSearchEngine = false;

        List<FContextMenu.MenuItem> items =
        [
            new(new MaterialIcon {Kind = MaterialIconKind.CardMultiple}, 1, "Instances", InstancesClick),
            new(new MaterialIcon {Kind = MaterialIconKind.BookmarkMultiple}, 1, "Bookmarks", BookmarkClick),
            new(new MaterialIcon {Kind = MaterialIconKind.History}, 1, "History", HistoryClick),
        ];
        
        ContextMenuPopup.Margin = new Thickness(32, 28, 0, 0);
        switch (TabManager.ActiveTabId)
        {
            case >= 0:
                ContextMenuPopup.SetItems(items
                    .Prepend(new(new MaterialIcon {Kind = MaterialIconKind.Cogs}, 1, "Settings", () => TabManager.SwapActiveTabTo(-2)))
                    .Append(new(new MaterialIcon {Kind = MaterialIconKind.Download}, 1, "Downloads", DownloadClick))
                    .Append(new(new MaterialIcon {Kind = MaterialIconKind.Puzzle}, 1, "Extensions", ExtensionsClick, false)));
                break;
            case -1:
                ContextMenuPopup.SetItems(items
                    .Prepend(new(new MaterialIcon {Kind = MaterialIconKind.Cogs}, 1, "Settings",() => TabManager.SwapActiveTabTo(-2)))
                    .Append(new(new MaterialIcon {Kind = MaterialIconKind.Pencil}, 1, "Edit Home", EditHomeClick)));
                break;
            case -2:
                ContextMenuPopup.SetItems(items);
                break;
        }
    }

    private void InstancesClick()
    {
        PopupContainer.Children.Clear();
        AppServer.Instances
            .Select(i =>
                {
                    var ic = new InstanceCard(i, i.Name == Instance.Name);
                    ic.OpenRequested += async () => await i.CreateWindow();
                    ic.TransferRequested += async () => await i.CreateWindow(
                        TabManager.GetAllTabs().Select(t => t.Value.Info.Url).ToArray(),
                        new Rect(AppWindow.Position.X, AppWindow.Position.Y, AppWindow.Size.Width, AppWindow.Size.Height),
                        StateFromWindow());
                    ic.CurrentTheme = CurrentTheme;
                    return ic;
                })
            .ToList()
            .ForEach(ic => PopupContainer.Children.Add(ic));
        
    }

    private void BookmarkClick()
    {
        PopupContainer.Children.Clear();
        Instance.Bookmarks
            .Select(b =>
                {
                    var bc = new BookmarkCard(b);
                    bc.NoteChanged += s => b.Note = s;
                    bc.RemoveRequested += () =>
                    {
                        Instance.Bookmarks.Remove(b);
                        PopupContainer.Children.Remove(bc);
                    };
                    bc.OnClick += () =>
                    {
                        TabManager.SwapActiveTabTo(TabManager.AddTab(b.Url));
                        PopupRootRoot.IsOpen = false;
                    };
                    bc.CurrentTheme = CurrentTheme;
                    return bc;
                })
            .ToList()
            .ForEach(bc => PopupContainer.Children.Add(bc));
        
        PopupRootRoot.IsOpen = true;
    }

    private void HistoryClick()
    {
        // throw new NotImplementedException();
    }

    private void ExtensionsClick()
    {
        ContextMenuPopup.SetItems(
            Instance
                .GetExtensions()
                .Select(e =>
                    {
                        if (e.Manifest is ExtensionManifestV3 manifestV3)
                        {
                            return new FContextMenu.MenuItem(
                                new Image
                                {
                                    Source = new BitmapImage(new Uri(Path.Combine(e.FolderPath, manifestV3.Icons?.Values?.FirstOrDefault()
                                                                                  ?? manifestV3.Action?.DefaultIcon?.Values?.FirstOrDefault()
                                                                                  ?? throw new Exception("No icon found for extension")))),
                                    Stretch = Stretch.Uniform,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                }, 1, 
                                manifestV3.Action.DefaultTitle??manifestV3.ShortName??manifestV3.Name,
                                () =>
                                {
                                    ExtensionPopupWebview.CoreWebView2.Navigate($"chrome-extension://{e.Id}/{manifestV3.Action?.DefaultPopup ?? ""}");
                                    ExtensionPopupRoot.IsOpen = true;
                                });
                        }
                        else if (e.Manifest is ExtensionManifestV2 manifestV2)
                        {
                            return new FContextMenu.MenuItem(
                                new Image
                                {
                                    Source = new BitmapImage(new Uri(Path.Combine(e.FolderPath, manifestV2.Icons?.Values?.FirstOrDefault() ??
                                        manifestV2.BrowserAction?.DefaultIcon?.Values?.FirstOrDefault()
                                        ?? throw new Exception("No icon found for extension")))),
                                    Stretch = Stretch.Uniform,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                }, 1, 
                                manifestV2.BrowserAction.DefaultTitle??manifestV2.ShortName??manifestV2.Name,
                                () =>
                                {
                                    ExtensionPopupWebview.CoreWebView2.Navigate($"chrome-extension://{e.Id}/{manifestV2.BrowserAction?.DefaultPopup ?? ""}");
                                    ExtensionPopupRoot.IsOpen = true;
                                });
                        }
                        else
                            throw new Exception("Unknown extension manifest type");
                    }
                )
        );
    }

    private void DownloadClick()
    {
        if (TabManager.TryGetTab(TabManager.ActiveTabId, out var tab))
            if (tab.Core.CoreWebView2.IsDefaultDownloadDialogOpen) 
                tab.Core.CoreWebView2.CloseDefaultDownloadDialog();
            else tab.Core.CoreWebView2.OpenDefaultDownloadDialog();
    }

    private void EditHomeClick()
    {
        HomePage.EditModeStart();
    }

    private bool _contextmenuUsedForSearchEngine;
    private void TopBar_OnEngineClicked()
    {
        if (_contextmenuUsedForSearchEngine)
        {
            ContextMenuPopup.SetItems([]);
            _contextmenuUsedForSearchEngine = false;
            return;
        }
        
        _contextmenuUsedForSearchEngine = true;
        
        ContextMenuPopup.Margin = new Thickness( TopBar.GetSearchEngineOffset() - 4, 4, 0, 0);
        ContextMenuPopup.SetItems(
            Enum.GetValues<InfoGetter.SearchEngine>()
                .Where(e => e != Instance.Cache.CurrentSearchEngine)
                .Prepend(Instance.Cache.CurrentSearchEngine)
                .Select(se => 
                    new FContextMenu.MenuItem(
                        new Image
                        {
                            Source = new BitmapImage(new Uri(InfoGetter.GetSearchEngineIcon(se))),
                            Stretch = Stretch.Uniform,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        },
                        2,
                        null/*InfoGetter.GetSearchEngineName(se)*/, 
                        () => SearchEngineChangeRequested?.Invoke(se)
                    )
                ), 22);
    }

    private void TopBar_OnBackClicked()
    {
        if (TabManager.TryGetTab(TabManager.ActiveTabId, out var tab))
            tab!.Core.GoBack();
    }
    
    private void TopBar_OnForwardClicked()
    {
        if (TabManager.TryGetTab(TabManager.ActiveTabId, out var tab))
            tab!.Core.GoForward();
    }

    private void TopBar_OnRefreshClicked()
    {
        if (TabManager.TryGetTab(TabManager.ActiveTabId, out var tab))
            tab!.Core.Reload();
    }

    private void ContextMenuPopup_OnOnClose()
    {
        _contextmenuUsedForSearchEngine = false;
    }
    
    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        //TODO
    }

    private void MainWindow_OnWindowStateChanged(object? sender, WindowState e)
    {
        TopBar.UpdateMaximizeRestore(e);
    }
    
    private void MainWindow_OnActivated(object sender, WindowActivatedEventArgs args)
    {
        
    }

    private void TopBar_OnToggleSidebarLock(bool isLocked)
    {
        Instance.Cache.LeftBarLocked = isLocked;
    }

    private void BorderGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        Debug.WriteLine(sender);
    }
}