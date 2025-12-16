using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI.Animations;
using FoxyBrowser716.Controls.Generic;
using FoxyBrowser716.Controls.HomePage;
using FoxyBrowser716.DataManagement;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.DataObjects.Complex;
using Material.Icons;
using Material.Icons.WinUI3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716.Controls.MainWindow;

[ObservableObject]
public sealed partial class NewLeftBar : UserControl
{
    [ObservableProperty] private partial Visibility TabsVisible { get; set; } = Visibility.Visible;
    [ObservableProperty] private partial Visibility WidgetsVisible { get; set; } = Visibility.Collapsed;

    
    private TabManager? TabManager;
    
    public NewLeftBar()
    {
        InitializeComponent();
        
        HomeCard.Icon.Child = new MaterialIcon { Kind = MaterialIconKind.Home };
        PinCard.Icon.Child = new MaterialIcon { Kind = MaterialIconKind.PinOutline };
        BookmarkCard.Icon.Child = new MaterialIcon { Kind = MaterialIconKind.BookmarkOutline };
        
        HomeCard.Label.Text = "Home";
        PinCard.Label.Text = "Pin Tab";
        BookmarkCard.Label.Text = "Bookmark Tab";
    }

    internal async Task Initialize(TabManager tabManager)
    {
        TabManager = tabManager;
        TabManager.ActiveTabChanged += TabManagerOnActiveTabChanged;
        
        
        PinCard.Visibility = TabManager.ActiveTabId < 0 ? Visibility.Collapsed : Visibility.Visible;
        BookmarkCard.Visibility = TabManager.ActiveTabId < 0 ? Visibility.Collapsed : Visibility.Visible;
        HomeCard.ForceHighlight = TabManager.ActiveTabId == -1;
        
        if (TabManager.ActiveTabId > 0 && TabManager.TryGetTab(TabManager.ActiveTabId, out var wt))
        {
            wt!.Info.PropertyChanged += InfoOnPropertyChanged;
        }
    }

    private void InfoOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WebsiteInfo.Url) 
            && sender is WebsiteInfo wi
            && PinCard.Icon.Child is MaterialIcon pmi
            && BookmarkCard.Icon.Child is MaterialIcon bmi)
        {
            pmi.Kind = TabManager!.Instance.Pins.Any(p => string.Equals(wi.Url, p.Url, StringComparison.CurrentCultureIgnoreCase)) 
                ? MaterialIconKind.Pin 
                : MaterialIconKind.PinOutline;
            
            bmi.Kind = TabManager.Instance.Bookmarks.Any(p => string.Equals(wi.Url, p.Url, StringComparison.CurrentCultureIgnoreCase)) 
                ? MaterialIconKind.Bookmark
                : MaterialIconKind.BookmarkOutline;
        }
    }

    private void TabManagerOnActiveTabChanged((int oldId, int newId) args) 
        => RefreshActiveTabUi(args.oldId, args.newId);
    

    private void RefreshActiveTabUi(int oldId, int newId)
    {
        PinCard.Visibility = newId < 0 ? Visibility.Collapsed : Visibility.Visible;
        BookmarkCard.Visibility = newId < 0 ? Visibility.Collapsed : Visibility.Visible;
        HomeCard.ForceHighlight = newId == -1;

        if (oldId >= 0 && TabManager!.TryGetTab(oldId, out var owt))
        {
            owt!.Info.PropertyChanged -= InfoOnPropertyChanged;
        }
        
        if (newId >= 0 && TabManager!.TryGetTab(newId, out var nwt))
        {
            nwt!.Info.PropertyChanged += InfoOnPropertyChanged;
            if (PinCard.Icon.Child is MaterialIcon pmi
                && BookmarkCard.Icon.Child is MaterialIcon bmi)
            {
                pmi.Kind = TabManager!.Instance.Pins.Any(p => string.Equals(nwt.Info.Url, p.Url, StringComparison.CurrentCultureIgnoreCase)) 
                    ? MaterialIconKind.Pin 
                    : MaterialIconKind.PinOutline;
            
                bmi.Kind = TabManager.Instance.Bookmarks.Any(p => string.Equals(nwt.Info.Url, p.Url, StringComparison.CurrentCultureIgnoreCase)) 
                    ? MaterialIconKind.Bookmark
                    : MaterialIconKind.BookmarkOutline;
            }
        }
    }

    [ObservableProperty] internal partial Theme CurrentTheme
    {
        get;
        set;
    } = DefaultThemes.LightMode;

    #region SidebarAnimator
    public bool LockSideBar { get; set; }
    
    internal async void OpenSideBar()
    {
        if (SideOpen) return;

        SideOpen = true;
        
        await AnimationBuilder.Create()
            .Size(Axis.X, 260, null, null, TimeSpan.FromSeconds(0.5), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml)
            .StartAsync(Root);
    }

    internal async void CloseSideBar()
    {
        if (!SideOpen) return;

        SideOpen = false;
        
        await AnimationBuilder.Create()
            .Size(Axis.X, 30, null, null, TimeSpan.FromSeconds(0.5), null, EasingType.Quintic, EasingMode.EaseIn, FrameworkLayer.Xaml)
            .StartAsync(Root);
    }

    private bool MouseOver;
    public bool SideOpen { get; private set; }
    private void Root_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = true;
        
        if (LockSideBar) return;
        
        var click = false;

        void LeftDown(object s, PointerRoutedEventArgs e)
        {
            click = true;
        }
        PointerPressed += LeftDown;
		
        Task.Delay(175).ContinueWith(_ =>
        {
            AppServer.UiDispatcherQueue.TryEnqueue(() => PointerPressed -= LeftDown);
            if (MouseOver && !SideOpen && !click)
                AppServer.UiDispatcherQueue.TryEnqueue(OpenSideBar);
        });
    }
    
    private void Root_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = false;
        
        if (LockSideBar) return;
        
        Task.Delay(175).ContinueWith(_ =>
        {
            if (!MouseOver && SideOpen)
                AppServer.UiDispatcherQueue.TryEnqueue(CloseSideBar);
        });
    }
    
    public void SetLockedState(bool locked)
    {
        LockSideBar = locked;
    }
    #endregion
    
    
    private void PinCard_OnClick(NewTabCard newTabCard)
    {
        if (newTabCard.Tag is WebsiteInfo wi)
        {
            TabManager?.SwapActiveTabTo(TabManager.AddTab(wi.Url));
        }
    }

    private void PinCard_OnCloseRequested(NewTabCard newTabCard)
    {
        if (newTabCard.Tag is WebsiteInfo wi)
        {
            TabManager?.Instance.Pins.Remove(wi);
        }
    }

    private void TabCard_OnClick(NewTabCard obj)
    {
        if (obj.Tag is WebviewTab wt)
        {
            TabManager?.SwapActiveTabTo(wt.Id);
        }
    }

    private void TabCard_OnCloseRequested(NewTabCard obj)
    {
        if (obj.Tag is WebviewTab wt)
        {
            TabManager?.RemoveTab(wt.Id);
        }
    }

    private void TabCard_OnDuplicateRequested(NewTabCard obj)
    {
        if (obj.Tag is WebviewTab wt)
        {
            TabManager?.SwapActiveTabTo(TabManager.AddTab(wt.Info.Url));
        }
    }

    private void HomeCard_OnOnClick(int id)
    {
        TabManager?.SwapActiveTabTo(id);
    }

    private void PinCard_OnOnClick(int obj)
    {
        if (TabManager!.ActiveTabId >= 0 && TabManager.TryGetTab(TabManager.ActiveTabId, out var tab))
        {
            if (TabManager.Instance.Pins.FirstOrDefault(p => string.Equals(p.Url, tab!.Info.Url, StringComparison.CurrentCultureIgnoreCase)) is { } pinToRemove)
            {
                TabManager.Instance.Pins.Remove(pinToRemove);
                
                if (PinCard.Icon.Child is MaterialIcon mi) 
                    mi.Kind = MaterialIconKind.PinOutline;
            }
            else
            {
                TabManager.Instance.Pins.Add(tab!.Info);
                
                if (PinCard.Icon.Child is MaterialIcon mi) 
                    mi.Kind = MaterialIconKind.Pin;
            }
        }
    }

    private void BookmarkCard_OnOnClick(int obj)
    {
        if (TabManager!.ActiveTabId >= 0 && TabManager.TryGetTab(TabManager.ActiveTabId, out var tab))
        {
            if (TabManager.Instance.Bookmarks.FirstOrDefault(p => string.Equals(p.Url, tab!.Info.Url, StringComparison.CurrentCultureIgnoreCase)) is { } pinToRemove)
            {
                TabManager.Instance.Bookmarks.Remove(pinToRemove);
                
                if (BookmarkCard.Icon.Child is MaterialIcon mi) 
                    mi.Kind = MaterialIconKind.BookmarkOutline;
            }
            else
            {
                TabManager.Instance.Bookmarks.Add(tab!.Info);
                
                if (BookmarkCard.Icon.Child is MaterialIcon mi) 
                    mi.Kind = MaterialIconKind.Bookmark;
            }
        }
    }

    private bool _editMode = false;
    public void ToggleEditMode(bool inEdit, HomePage.HomePage home)
    {
        if (inEdit && home is not null)
        {
            
            _editMode = true;
            
            Widgets.Children.Clear();
            EditOptions.Children.Clear();
            
            HomeCard.Visibility = Visibility.Collapsed;
            BookmarkCard.Visibility = Visibility.Collapsed;
            PinCard.Visibility = Visibility.Collapsed;

            var groupedWidgetOptions = home.GetWidgetOptions().OrderBy(w => w.name).GroupBy(w => w.category);
            var homeOptions = home.GetHomeOptions();

            foreach (var g in groupedWidgetOptions)
            {
                var category = g.Key;
                var widgets = g.ToList();

                var groupCard = new TabGroupCard(category.GetIcon(), category.GetName());
                foreach (var w in widgets)
                {
                    var card = new TabCard(w.icon, w.name);
                    card.CurrentTheme = CurrentTheme;
                    card.OnClick += async _ => await home.AddWidgetClicked(w.name);
                    groupCard.AddTabCard(card);
                }

                groupCard.CurrentTheme = CurrentTheme;
                Widgets.Children.Add(groupCard);
            }

            foreach (var o in homeOptions)
            {
                var card = new TabCard(o.icon, o.name);
                card.CurrentTheme = CurrentTheme;
                card.OnClick += async _ => await home.OptionClicked(o.type);
                EditOptions.Children.Add(card);
            }
            
            TabsVisible = Visibility.Collapsed;
            WidgetsVisible = Visibility.Visible;
        }
        else if (!inEdit)
        {
            _editMode = false;
            
            TabsVisible = Visibility.Visible;
            WidgetsVisible = Visibility.Collapsed;
            
            HomeCard.Visibility = Visibility.Visible;
            BookmarkCard.Visibility = TabManager!.ActiveTabId >= 0 ? Visibility.Visible : Visibility.Collapsed;
            PinCard.Visibility = TabManager.ActiveTabId >= 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else
            throw new Exception("Home is null, cannot enter edit mode");
    }
    
    private NewTabCard? _draggingPinCard;
    private int _draggingPinIndex = -1;
    private double _dragStartY;

    private void NewTabCard_OnOnDragStarted(NewTabCard obj, ManipulationStartedRoutedEventArgs arg)
    {
        if (obj.Tag is WebsiteInfo wi && TabManager?.Instance.Pins is { } pins)
        {
            _draggingPinCard = obj;
            _draggingPinIndex = pins.IndexOf(wi);
            _dragStartY = arg.Position.Y;
        
            obj.Opacity = 0.6;
            
            arg.Handled = true;
            obj.InDrag = true;
        }
    }

    private void NewTabCard_OnOnDragMoved(NewTabCard obj, ManipulationDeltaRoutedEventArgs arg)
    {
        if (_draggingPinCard != obj || obj.Tag is not WebsiteInfo wi) return;
        if (TabManager?.Instance.Pins is not { } pins) return;
    
        var currentIndex = pins.IndexOf(wi);
        if (currentIndex < 0) return;
    
        var deltaY = arg.Cumulative.Translation.Y;
        var cardHeight = obj.ActualHeight;
        var indexDelta = (int)(deltaY / cardHeight);
        var targetIndex = Math.Clamp(_draggingPinIndex + indexDelta, 0, pins.Count - 1);
    
        if (targetIndex != currentIndex)
        {
            pins.Move(currentIndex, targetIndex);
        }
    }

    private void NewTabCard_OnOnDragCompleted(NewTabCard obj, ManipulationCompletedRoutedEventArgs arg)
    {
        if (_draggingPinCard != null)
        {
            _draggingPinCard.Opacity = 1.0;
            obj.InDrag = false;
        }
    
        _draggingPinCard = null;
        _draggingPinIndex = -1;
    }
}
