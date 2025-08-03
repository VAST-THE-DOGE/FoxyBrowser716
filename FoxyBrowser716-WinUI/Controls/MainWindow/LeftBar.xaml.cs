using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using FoxyBrowser716_WinUI.Controls.Generic;
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

        HomeCard.Icon.Child = new MaterialControlIcon { Kind = MaterialIconKind.Home };
        PinCard.Icon.Child = new MaterialControlIcon { Kind = MaterialIconKind.PinOutline };
        BookmarkCard.Icon.Child = new MaterialControlIcon { Kind = MaterialIconKind.BookmarkOutline };
        
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
                            Pins.Children.Add(card);
                            card.ShowDuplicate = false;
                            card.CurrentTheme = CurrentTheme;
                            card.OnClick += PinCardOnClick(websiteInfo);
                            card.CloseRequested += PinCardOnClose(websiteInfo);
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
                            Pins.Children.Add(card);
                            card.ShowDuplicate = false;
                            card.CurrentTheme = CurrentTheme;
                            card.OnClick += PinCardOnClick(newWebsiteInfo);
                            card.CloseRequested += PinCardOnClose(newWebsiteInfo);
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
                            Pins.Children.Remove(card);
                            Pins.Children.Insert(e.NewStartingIndex, card);
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                PinCards.Clear();
                Pins.Children.Clear();
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
            Tabs.Children.Remove(card);
    }

    private void TabManagerOnTabAdded(WebviewTab tab)
    {
        var card = new TabCard(tab);
        card.CloseRequested += TabManager!.RemoveTab;
        card.DuplicateRequested += CardOnDuplicateRequested;
        card.OnClick += TabManager!.SwapActiveTabTo;
        if (TabCards.TryAdd(tab.Id, card))
            Tabs.Children.Add(card);
    }

    private void CardOnDuplicateRequested(int id)
    {
        if (TabManager!.TryGetTab(id, out var tab))
            TabManager.SwapActiveTabTo(TabManager.AddTab(tab!.Info.Url));
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
        Root.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColor);
        Root.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        
        Div.Background = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);

        HomeCard.CurrentTheme = CurrentTheme;
        PinCard.CurrentTheme = CurrentTheme;
        BookmarkCard.CurrentTheme = CurrentTheme;
        
        foreach (TabCard card in Tabs.Children)
            card.CurrentTheme = CurrentTheme;
        foreach (TabCard card in Pins.Children)
            card.CurrentTheme = CurrentTheme;
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
}
