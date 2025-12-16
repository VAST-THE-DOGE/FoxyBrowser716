using System.Diagnostics;
using System.Net.Http;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using FoxyBrowser716.Controls.Generic;
using FoxyBrowser716.DataManagement;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.DataObjects.Complex;
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
using FoxyBrowser716.Controls.Helpers;
using FoxyBrowser716.DataObjects.Settings;
using FoxyBrowser716.ErrorHandeler;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Web.WebView2.Core;


// using CommunityToolkit.WinUI.Helpers;
//

namespace FoxyBrowser716.Controls.MainWindow;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public TabManager TabManager { get; private set; }
    public Instance Instance { get; private set; }

    public Action<InfoGetter.SearchEngine> SearchEngineChangeRequested;
    
    // private VisualCaptureHelper? _captureHelper;
    
    private MainWindow()
    {
        InitializeComponent();
        
        AppWindow.SetIcon(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, "Assets", "Foxybrowser716.ico"));
        
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
        
        await Task.WhenAll(LeftBar.Initialize(TabManager), HomePage.Initialize(this), ChatWindow.Initialize(this), SettingsPage.Initialize(this));
        
        // link events from tab manager
        TabManager.ActiveTabChanged += TabManagerOnActiveTabChanged;
        TabManager.TabAdded += TabManagerOnTabAdded;
        TabManager.TabRemoved += TabManagerOnTabRemoved;
        
        // link events from chat window
        ChatWindow.CloseRequested += () => TabHolder.Margin = TabHolder.Margin with { Right = 0 };
        
        // link events from the instance
        instance.Cache.PropertyChanged += (_, _) => HandleCacheChanged();
        
        // link events from home page
        HomePage.ToggleEditMode += HomePageOnToggleEditMode;
        
        TabManager.TryGetTab(TabManager.ActiveTabId, out var tab);
        RefreshCurrentTabUi(tab, TabManager.ActiveTabId < 0 ? TabManager.ActiveTabId : null);
        
        await ExtensionPopupWebview.EnsureCoreWebView2Async(TabManager.WebsiteEnvironment);
        await Instance.SetupExtensionSupport(ExtensionPopupWebview); // loads up those extensions!
        ExtensionPopupWebview.CoreWebView2.NewWindowRequested +=
            (_, args2) =>
            {
                TabManager.SwapActiveTabTo(TabManager.AddTab(args2.Uri));
                args2.Handled = true;
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
            ExtensionPopupWebview.Width = 300;//size.width;
            ExtensionPopupWebview.Height = 500; //size.height;
        };
        
        // _captureHelper = new VisualCaptureHelper(TabHolder, BlurredBackgroundGrid);

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
        ExtensionPopupRoot.IsOpen = false;
        ExtensionPopupWebview.NavigateToString(""); // clear it
        
        if (pair.newId < 0)
            RefreshCurrentTabUi(null, pair.newId);
        else if (TabManager.TryGetTab(pair.newId, out var tab))
            RefreshCurrentTabUi(tab);
        else
            throw new Exception($"Could not retrieve tab with id '{pair.newId}'");
        
        //TODO test and fine tune
        
        // apply fading:
        // var fadeOut = AnimationBuilder.Create();
        // var fadeIn = AnimationBuilder.Create();
        
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
        
        // if (pair.newId == -1)
        // {
        //     Canvas.SetZIndex(HomePage, 1);
        //     fadeIn.Opacity(1, 0, null, TimeSpan.FromSeconds(0.25), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml);
        //     _ = fadeIn.StartAsync(HomePage);
        // }
        // else if (pair.newId == -2)
        // {
        //     Canvas.SetZIndex(SettingsPage, 1);
        //     fadeIn.Opacity(1, 0, null, TimeSpan.FromSeconds(0.25), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml);
        //     _ = fadeIn.StartAsync(SettingsPage);
        // } 
        // else if (TabManager.TryGetTab(pair.newId, out var newTab))
        // {
        //     Canvas.SetZIndex(newTab!.Core, 1);
        //     fadeIn.Opacity(1, 0, null, TimeSpan.FromSeconds(0.25), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml);
        //     _ = fadeIn.StartAsync(newTab!.Core);
        // }
    }

    internal Theme CurrentTheme
    {
        get;
        set
        {
            field = value;
            ApplyTheme();
        }
    } = DefaultThemes.DaybreakFoxy;

    private void ApplyTheme()
    {
        //TODO: remove apply theme from other constructors
        TopBar.CurrentTheme = CurrentTheme;
        LeftBar.CurrentTheme = CurrentTheme;
        ContextMenuPopup.CurrentTheme = CurrentTheme;
        HomePage.CurrentTheme = CurrentTheme;
        ChatWindow.CurrentTheme = CurrentTheme;
        SettingsPage.CurrentTheme = CurrentTheme;

        SuggestionPanel.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        SuggestionPanel.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);
        
        PopupRoot.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        PopupRoot.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);
        
        CenterPopupRoot.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        CenterPopupRoot.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);
        CenterPopupCloseButton.CurrentTheme = CurrentTheme;
        
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
            //TODO: not stable enough.
            //new(new MaterialIcon {Kind = MaterialIconKind.Assistant}, 1, "Chat Window", AssistantClick),
            new(new MaterialIcon {Kind = MaterialIconKind.CardMultiple}, 1, "Instances", InstancesClick),
            new(new MaterialIcon {Kind = MaterialIconKind.BookmarkMultiple}, 1, "Bookmarks", BookmarkClick),
            //TODO: not implemented yet.
            //new(new MaterialIcon {Kind = MaterialIconKind.History}, 1, "History", HistoryClick),
            new(new MaterialIcon {Kind = MaterialIconKind.Puzzle}, 1, "Extensions", ExtensionsClick, false),
        ];
        
        ContextMenuPopup.Margin = new Thickness(32, 28, 0, 0);
        switch (TabManager.ActiveTabId)
        {
            case >= 0:
                ContextMenuPopup.SetItems(items
                    .Prepend(new(new MaterialIcon { Kind = MaterialIconKind.Cogs }, 1, "Settings",
                        () => TabManager.SwapActiveTabTo(-2)))
                    .Append(new(new MaterialIcon { Kind = MaterialIconKind.Download }, 1, "Downloads", DownloadClick)));
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

    private void AssistantClick()
    {
        ChatWindow.Visibility = Visibility.Visible;
        TabHolder.Margin = TabHolder.Margin with { Right = 450 };
    }

    private void InstancesClick()
    {
        PopupContainer.Children.Clear();
        AppServer.Instances
            .Select(i =>
                {
                    var ic = new InstanceCard(i, i.Name == Instance.Name);
                    ic.OpenRequested += async () => await i.CreateWindow();
                    ic.TransferRequested += async () =>
                    {
                        var currentPos = new Rect(AppWindow.Position.X, AppWindow.Position.Y, AppWindow.Size.Width, AppWindow.Size.Height);
                        var tabUrls = TabManager
                            .GetAllTabs()
                            .Select(t => t.Value.Info.Url)
                            .ToArray();
                        
                        await i.CreateWindow(tabUrls, currentPos, StateFromWindow());
                        this.Close();
                    };
                    ic.CurrentTheme = CurrentTheme;
                    return ic;
                })
            .ToList()
            .ForEach(ic => PopupContainer.Children.Add(ic));
        
        PopupRootRoot.IsOpen = true;
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
        
        // CenterPopupContainer.Children.Clear();
        // ////////////////////////////////////////
        // /// TESTING TEMP STUFF
        // new List<ISetting>
        // {
        //     new BoolSetting("test", "test test some more test...", true),
        //     new BoolSetting("test2", "test test some more test...", false),
        //     new ComboSetting("test3", "test test", 1, ("a", 0), ("b", 1), ("c", 2), ("d", 3)),
        //     new ColorSetting("test4", "test test some more test...", Colors.Red),
        //     new DecimalSetting("test5a", "test test some more test...", 1.23m),
        //     new IntSetting("test5b", "test test some more test...", 123),
        //     new DividerSetting(),
        //     new SliderSetting("test6", "test test some more test...", 50, 1, 100),
        //     new FilePickerSetting("test7", "test test some more test...", "test", true),
        // }
        // .ForEach(s =>
        // {
        //     var e = s.GetEditor(this);
        //     e.CurrentTheme = CurrentTheme;
        //     CenterPopupContainer.Children.Add(e);
        // });
        //
        // CenterPopupRootRoot.IsOpen = true;

        throw new NotImplementedException();
    }

    private void ExtensionsClick()
    {
        ContextMenuPopup.SetItems(
            Instance
                .GetSavedExtensions()
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
                                manifestV3.Action?.DefaultTitle??manifestV3.ShortName??manifestV3.Name,
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
                                manifestV2.BrowserAction?.DefaultTitle??manifestV2.ShortName??manifestV2.Name,
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
        // _captureHelper?.Dispose();
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

    private void CenterPopupCloseButton_OnOnClick(object sender, RoutedEventArgs e)
    {
        CenterPopupRootRoot.IsOpen = false;
    }

    public event Action? PopupClosed;
    private void CenterPopupRootRoot_OnClosed(object? sender, object e)
    {
        PopupClosed?.Invoke();
    }
    
    public void OpenSettings(List<ISetting> settings)
    {
        CenterPopupContainer.Children.Clear();
        
        if (settings.Count == 0) return;
        
        settings.ForEach(s =>
            {
                var e = s.GetEditor(this);
                e.CurrentTheme = CurrentTheme;
                CenterPopupContainer.Children.Add(e);
            });
        
        CenterPopupRootRoot.IsOpen = true;
    }

    private readonly HttpClient _httpClient = new();
    private async void TopBar_OnSearchTextChanged(string searchText) //TODO: optimize this jumble letters 
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            SuggestionPanel.Visibility = Visibility.Collapsed;
            return;
        }
        
        List<string> suggestions = [];
        
        for (var i = 0; i < 3; i++)
        {
            try
            {
                var url = InfoGetter.GetSearchCompletionUrl(searchText);
                var json = await _httpClient.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                if (data.GetArrayLength() > 1)
                {
                    suggestions = [];
                    foreach (var item in data[1].EnumerateArray())
                    {
                        suggestions.Add(item.GetString());
                    }
                
                }
                
                break;
            }
            catch (Exception e)
            {
                ErrorInfo.AddError(e);
                Debug.WriteLine(e);
            }
        }

        var offsetWidth = TopBar.GetSearchBarOffsetAndWidth();
        SuggestionPanel.Margin = new Thickness(offsetWidth.offset, 
            SuggestionPanel.Margin.Top, SuggestionPanel.Margin.Right, 
            SuggestionPanel.Margin.Bottom);
        SuggestionPanel.Width = offsetWidth.width;
        SuggestionPanel.Visibility = Visibility.Visible;
        
        SearchCompletions.Children.Clear();
        if (suggestions.Count == 0)
            SearchCompletionsLabel.Visibility = Visibility.Collapsed;
        else
        {
            SearchCompletionsLabel.Visibility = Visibility.Visible;
            foreach (var suggestion in suggestions)
            {
                var button = new FTextButton {
                    ButtonText = suggestion, CurrentTheme = CurrentTheme,
                    CornerRadius = new CornerRadius(8), Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left,
                };
                
                button.OnClick += (_, _) => TopBar_OnSearchClicked(suggestion);
                SearchCompletions.Children.Add(button);
            }
        }
        
        var openTabs = TabManager.Groups.SelectMany(g => g.Tabs)
            .Concat(TabManager.Tabs)
            .Where(t => t.Info.Title.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        t.Info.Url.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        
        OpenTabSuggestions.Children.Clear();
        if (openTabs.Count == 0)
            OpenTabsLabel.Visibility = Visibility.Collapsed;
        else
        {
            OpenTabsLabel.Visibility = Visibility.Visible;
            foreach (var ot in openTabs)
            {
                var button = new FTextButton {
                    Icon = UrlToImageControlConverter.StaticConvert(ot.Info.FavIconUrl), 
                    ButtonText = $"{ot.Info.Title} - {ot.Info.Url}", CurrentTheme = CurrentTheme,
                    CornerRadius = new CornerRadius(8), Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left,
                };
                
                button.OnClick += (_, _) => TabManager.SwapActiveTabTo(ot.Id);
                OpenTabSuggestions.Children.Add(button);
            }
        }

        var bookmarks = Instance.Bookmarks
            .Where(b => b.Url.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                || b.Title.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        
        BookmarkSuggestions.Children.Clear();
        if (bookmarks.Count == 0)
            BookmarkSuggestionsLabel.Visibility = Visibility.Collapsed;
        else
        {
            BookmarkSuggestionsLabel.Visibility = Visibility.Visible;
            foreach (var book in bookmarks)
            {
                var button = new FTextButton {
                    Icon = UrlToImageControlConverter.StaticConvert(book.FavIconUrl), 
                    ButtonText = $"{book.Title} - {book.Url}", CurrentTheme = CurrentTheme,
                    CornerRadius = new CornerRadius(8), Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left,
                };
                
                button.OnClick += (_, _) => TopBar_OnSearchClicked(book.Url);
                BookmarkSuggestions.Children.Add(button);
            }
        }

        var pins = Instance.Pins
            .Where(b => b.Url.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                        || b.Title.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        
        PinSuggestions.Children.Clear();
        if (pins.Count == 0)
            PinSuggestionsLabel.Visibility = Visibility.Collapsed;
        else
        {
            PinSuggestionsLabel.Visibility = Visibility.Visible;
            foreach (var pin in pins)
            {
                var button = new FTextButton {
                    Icon = UrlToImageControlConverter.StaticConvert(pin.FavIconUrl), 
                    ButtonText = $"{pin.Title} - {pin.Url}", CurrentTheme = CurrentTheme,
                    CornerRadius = new CornerRadius(8), Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left,
                };
                
                button.OnClick += (_, _) => TopBar_OnSearchClicked(pin.Url);
                PinSuggestions.Children.Add(button);
            }
        }
        
        List<string> history = [];
        
        HistorySuggestions.Children.Clear();
        if (history.Count == 0)
            HistorySuggestionsLabel.Visibility = Visibility.Collapsed;
        else
        {
            HistorySuggestionsLabel.Visibility = Visibility.Visible;
            //TODO
        }
    }

    private void TopBar_OnSearchBarUnfocused()
    {
        SuggestionPanel.Visibility = Visibility.Collapsed;
    }

    private void TopBar_OnUpdateClicked()
    {
        // throw new NotImplementedException();
        //TODO open MS Store
    }

    private void HomePage_OnHomeImageUrlChanged(Uri? obj)
    {
        BackImage.Source = obj is null ? null : new BitmapImage(new Uri(obj.ToString())); { };
    }
}