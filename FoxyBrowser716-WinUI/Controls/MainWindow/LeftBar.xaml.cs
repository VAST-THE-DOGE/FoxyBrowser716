using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using FoxyBrowser716_WinUI.Controls.Generic;
using FoxyBrowser716_WinUI.Controls.HomePage;
using FoxyBrowser716_WinUI.DataManagement;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using FoxyBrowser716_WinUI.DataObjects.Complex;
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

namespace FoxyBrowser716_WinUI.Controls.MainWindow;

public sealed partial class LeftBar : UserControl
{
    private TabManager? TabManager;

    private Dictionary<int, TabCard> TabCards = [];
    private Dictionary<int, TabCard> PinCards = [];
    private static int _pinCounter;

    
    public LeftBar()
    {
        InitializeComponent();
        ApplyTheme();

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
        TabManager.TabAdded += TabManagerOnTabAdded;
        TabManager.TabRemoved += TabManagerOnTabRemoved;
        TabManager.ActiveTabChanged += TabManagerOnActiveTabChanged;
        TabManager.Instance.Pins.CollectionChanged += PinsOnCollectionChanged;

        HomeCard.OnClick += TabManager.SwapActiveTabTo;

        foreach (WebsiteInfo newWebsiteInfo in TabManager.Instance.Pins)
        {
            var pinId = Interlocked.Increment(ref _pinCounter);
            var card = new TabCard(pinId, newWebsiteInfo);
            if (PinCards.TryAdd(pinId, card))
            {
                Pins.Children.Add(card);
                card.ShowDuplicate = false;
                card.CurrentTheme = CurrentTheme;
                card.OnClick += PinCardOnClick(newWebsiteInfo);
                card.CloseRequested += PinCardOnClose(newWebsiteInfo);
            }
        }
        
        //TODO: load pins + tabs
    }

    private void PinsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (WebsiteInfo websiteInfo in e.NewItems)
                    {
                        var pinId = Interlocked.Increment(ref _pinCounter);
                        var card = new TabCard(pinId, websiteInfo);
                        if (PinCards.TryAdd(pinId, card))
                        {
                            card.ShowDuplicate = false;
                            card.CurrentTheme = CurrentTheme;
                            card.OnClick += PinCardOnClick(websiteInfo);
                            card.CloseRequested += PinCardOnClose(websiteInfo);
                            if (_editMode)
                                _pinsCache.Add(card);
                            else
                                Pins.Children.Add(card);
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (WebsiteInfo websiteInfo in e.OldItems)
                    {
                        var cardToRemove = PinCards.FirstOrDefault(kvp => 
                            kvp.Value.Tag == websiteInfo).Value;
                        if (cardToRemove != null)
                        {
                            var pinId = PinCards.FirstOrDefault(kvp => kvp.Value == cardToRemove).Key;
                            if (PinCards.Remove(pinId))
                            {
                                if (_editMode)
                                    _pinsCache.Remove(cardToRemove);
                                else
                                    Pins.Children.Remove(cardToRemove);
                            }
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null && e.NewItems != null)
                {
                    foreach (WebsiteInfo oldWebsiteInfo in e.OldItems)
                    {
                        var cardToRemove = PinCards.FirstOrDefault(kvp => 
                            kvp.Value.Tag == oldWebsiteInfo).Value;
                        if (cardToRemove != null)
                        {
                            var pinId = PinCards.FirstOrDefault(kvp => kvp.Value == cardToRemove).Key;
                            if (PinCards.Remove(pinId))
                            {
                                if (_editMode)
                                    _pinsCache.Remove(cardToRemove);
                                else
                                    Pins.Children.Remove(cardToRemove);
                            }
                        }
                    }
                    
                    foreach (WebsiteInfo newWebsiteInfo in e.NewItems)
                    {
                        var pinId = Interlocked.Increment(ref _pinCounter);
                        var card = new TabCard(pinId, newWebsiteInfo);
                        if (PinCards.TryAdd(pinId, card))
                        {
                            card.ShowDuplicate = false;
                            card.CurrentTheme = CurrentTheme;
                            card.OnClick += PinCardOnClick(newWebsiteInfo);
                            card.CloseRequested += PinCardOnClose(newWebsiteInfo);
                            if (_editMode)
                                _pinsCache.Add(card);
                            else
                                Pins.Children.Add(card);
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Move:
                if (e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0 && e.NewItems != null)
                {
                    foreach (WebsiteInfo websiteInfo in e.NewItems)
                    {
                        var card = PinCards.FirstOrDefault(kvp => 
                            kvp.Value.Tag == websiteInfo).Value;
                        if (card != null)
                        {
                            if (_editMode)
                            {
                                _pinsCache.Remove(card);
                                _pinsCache.Insert(e.NewStartingIndex, card);
                            }
                            else
                            {
                                Pins.Children.Remove(card);
                                Pins.Children.Insert(e.NewStartingIndex, card);
                            }
                            
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                PinCards.Clear();
                Pins.Children.Clear();
                _pinsCache.Clear();
                break;
        }
    }

    private Action<int>? PinCardOnClose(WebsiteInfo websiteInfo)
    {
        return _ => TabManager!.Instance.Pins.Remove(websiteInfo);
    }

    private Action<int>? PinCardOnClick(WebsiteInfo websiteInfo)
    {
        return _ => TabManager!.SwapActiveTabTo(TabManager.AddTab(websiteInfo.Url));
    }

    private void TabManagerOnActiveTabChanged((int oldId, int newId) pair)
    {
        PinCard.Visibility = pair.newId < 0 ? Visibility.Collapsed : Visibility.Visible;
        BookmarkCard.Visibility = pair.newId < 0 ? Visibility.Collapsed : Visibility.Visible;
        
        //TODO: update if it is pin or unpin here

        switch (pair.oldId)
        {
            case -2:
                break;
            case -1:
                HomeCard.ForceHighlight = false;
                break;
            case >= 0:
                if (TabCards.TryGetValue(pair.oldId, out var card))
                    card.ForceHighlight = false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        switch (pair.newId)
        {
            case -2:
                break;
            case -1:
                HomeCard.ForceHighlight = true;
                break;
            case >= 0:
                if (TabCards.TryGetValue(pair.newId, out var card))
                    card.ForceHighlight = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void TabManagerOnTabRemoved(WebviewTab tab)
    {
        if (TabCards.Remove(tab.Id, out var card)) 
            if (_editMode)
                _tabsCache.Remove(card);
            else
                Tabs.Children.Remove(card);
    }

    private void TabManagerOnTabAdded(WebviewTab tab)
    {
        var card = new TabCard(tab);
        card.CloseRequested += TabManager!.RemoveTab;
        card.DuplicateRequested += CardOnDuplicateRequested;
        card.OnClick += TabManager!.SwapActiveTabTo;
        if (TabCards.TryAdd(tab.Id, card))
            if (_editMode)
                _tabsCache.Add(card);
            else
                Tabs.Children.Add(card);
    }

    private void CardOnDuplicateRequested(int id)
    {
        if (TabManager!.TryGetTab(id, out var tab))
            TabManager.SwapActiveTabTo(TabManager.AddTab(tab!.Info.Url));
    }

    internal Theme CurrentTheme
    {
        get;
        set
        {
            field = value;
            ApplyTheme();
        }
    } = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        Root.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColor);
        Root.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        
        Div.Background = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);

        HomeCard.CurrentTheme = CurrentTheme;
        PinCard.CurrentTheme = CurrentTheme;
        BookmarkCard.CurrentTheme = CurrentTheme;
        
        foreach (var card in Tabs.Children)
            switch (card)
            {
                case TabGroupCard groupCard:
                    groupCard.CurrentTheme = CurrentTheme;
                    break;
                case TabCard tabCard:
                    tabCard.CurrentTheme = CurrentTheme;
                    break;
            }
        foreach (var card in Pins.Children)
            if (card is TabCard tabCard)
                tabCard.CurrentTheme = CurrentTheme;
    }

    //TODO: test
    private double oldHeight;
    private void LeftBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Math.Abs(oldHeight - e.NewSize.Height) < 0.5) return;
        
        oldHeight = e.NewSize.Height;
        
        const int controlHeight = 26;
        
        var homeCardHeight = HomeCard.ActualHeight;
        var dividerHeight = Div.ActualHeight;
        var pinCardHeight = PinCard.ActualHeight;
        var bookmarkCardHeight = BookmarkCard.ActualHeight;
        var totalHeightAvailable = e.NewSize.Height - homeCardHeight - dividerHeight - pinCardHeight - bookmarkCardHeight;

        
        var tabs = Tabs.Children.Count;
        var pins = Tabs.Children.Count;
        
        var tabRequestedHeight = tabs * controlHeight;
        var pinRequestedHeight = pins * controlHeight;
        
        // easy case, both can fit:
        if (pinRequestedHeight + tabRequestedHeight <= totalHeightAvailable)
        {
            PinsRow.MaxHeight = pinRequestedHeight;
        }
        // pins are larger than tabs and not too many tabs (under 75% of available height), cap pins.
        else if (pinRequestedHeight >= tabRequestedHeight && tabRequestedHeight < totalHeightAvailable * 0.75)
        {
            PinsRow.MaxHeight = totalHeightAvailable - tabRequestedHeight;
        }
        // tabs are larger than pins and not too many pins (under 50% of available height), cap tabs.
        else if (pinRequestedHeight < tabRequestedHeight && pinRequestedHeight < totalHeightAvailable * 0.5)
        {
            PinsRow.MaxHeight = pinRequestedHeight;
        }
        // cap both
        else
        {
            var actualTabsHeight = Math.Min(104, totalHeightAvailable * 0.75);
            PinsRow.MaxHeight = Math.Max(0, totalHeightAvailable - actualTabsHeight);
        }
    }
    
    internal void OpenSideBar()
    {
        if (SideOpen) return;

        SideOpen = true;
    
        Root.Width = 260; //TODO: temp
    }

    internal void CloseSideBar()
    {
        if (!SideOpen) return;

        SideOpen = false;

        Root.Width = 30; //TODO: temp
    }

    private bool MouseOver;
    public bool SideOpen { get; private set; }
    private void Root_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = true;
        var click = false;

        void LeftDown(object s, PointerRoutedEventArgs e)
        {
            click = true;
        }
        PointerPressed += LeftDown;
		
        Task.Delay(250).ContinueWith(_ =>
        {
            AppServer.UiDispatcherQueue.TryEnqueue(() => PointerPressed -= LeftDown);
            if (MouseOver && !SideOpen && !click)
                AppServer.UiDispatcherQueue.TryEnqueue(OpenSideBar);
        });
    }
    
    private void Root_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = false;
        Task.Delay(300).ContinueWith(_ =>
        {
            if (!MouseOver && SideOpen)
                AppServer.UiDispatcherQueue.TryEnqueue(CloseSideBar);
        });
    }

    private void Root_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        //throw new NotImplementedException();
    }

    private void Root_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        //throw new NotImplementedException();
    }

    private void Root_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        //throw new NotImplementedException();
    }

    private List<UIElement> _tabsCache = [];
    private List<UIElement> _pinsCache = [];
    private bool _editMode;
    public void ToggleEditMode(bool inEdit, HomePage.HomePage? home)
    {
        if (inEdit && home is not null)
        {
            _tabsCache.AddRange(Tabs.Children);
            _pinsCache.AddRange(Pins.Children);
            
            _editMode = true;
            
            Tabs.Children.Clear();
            Pins.Children.Clear();
            
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
                    card.OnClick += async _ => await home.AddWidgetClicked(w.name);
                    groupCard.AddTabCard(card);
                }

                groupCard.CurrentTheme = CurrentTheme;
                Tabs.Children.Add(groupCard);
            }

            foreach (var o in homeOptions)
            {
                var card = new TabCard(o.icon, o.name);
                card.OnClick += async _ => await home.OptionClicked(o.type);
                Pins.Children.Add(card);
            }
        }
        else if (!inEdit)
        {
            Tabs.Children.Clear();
            Pins.Children.Clear();
            
            _editMode = false;
            
            foreach (var c in _tabsCache)
                Tabs.Children.Add(c);
            
            foreach (var c in _pinsCache)
                Pins.Children.Add(c);
            
            _tabsCache.Clear();
            _pinsCache.Clear();
            
            HomeCard.Visibility = Visibility.Visible;
            BookmarkCard.Visibility = TabManager!.ActiveTabId >= 0 ? Visibility.Visible : Visibility.Collapsed;
            PinCard.Visibility = TabManager.ActiveTabId >= 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else
            throw new Exception("Home is null, cannot enter edit mode");
    }
}
