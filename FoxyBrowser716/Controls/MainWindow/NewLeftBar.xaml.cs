using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
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
    
    [ObservableProperty] private partial Visibility MoveTipsVisible { get; set; } = Visibility.Collapsed;
    
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

    private void TabsList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0 && e.Items[0] is WebviewTab tab)
        {
            e.Data.Properties.Add("DragType", "Tab");
            e.Data.Properties.Add("DragItem", tab);
            e.Data.Properties.Add("SourceManager", TabManager);
        }
    }

    private void GroupsList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0 && e.Items[0] is TabGroup group)
        {
            e.Data.Properties.Add("DragType", "TabGroup");
            e.Data.Properties.Add("DragItem", group);
            e.Data.Properties.Add("SourceManager", TabManager);
        }
    }
    
    
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
    
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
    
    private void TabsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        MoveTipsVisible = Visibility.Collapsed;
        if (args.DropResult == DataPackageOperation.None)
        {
            // Drop occurred outside the app or was cancelled
            if (args.Items.FirstOrDefault() is WebviewTab tab)
            {
                // Create new window with this tab
                var cursorPosition = new Point(0, 0);
                if (GetCursorPos(out var lpPoint))
                {
                    cursorPosition.X = lpPoint.X;
                    cursorPosition.Y = lpPoint.Y;
                    
                    TabManager?.CreateWindowWithTab(tab, new Rect(cursorPosition, new Size(250, 100)));
                }
                else
                    throw new Exception("Failed to get cursor position for drop operation outside app.");
            }
        }
    }

    private void GroupsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        MoveTipsVisible = Visibility.Collapsed;
        if (args.DropResult == DataPackageOperation.None)
        {
            if (args.Items.FirstOrDefault() is TabGroup group)
            {
                // Create new window with this group
                var cursorPosition = new Point(0, 0);
                if (GetCursorPos(out var lpPoint))
                {
                    cursorPosition.X = lpPoint.X;
                    cursorPosition.Y = lpPoint.Y;
                    
                    TabManager?.CreateWindowWithGroup(group, new Rect(cursorPosition, new Size(250, 100)));
                }
            }
        }
    }

    private void TabsList_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("SourceManager", out var smObj) && smObj is TabManager sourceManager)
        {
            if (TabManager?.Instance.Name != sourceManager.Instance.Name)
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }
        }

        e.AcceptedOperation = DataPackageOperation.Move;
        
        if (e.DataView.Properties.TryGetValue("DragType", out var typeObj) && typeObj is string type)
        {
            if (type == "Tab")
            {
                if (e.DataView.Properties.ContainsKey("SourceGroupId"))
                    e.DragUIOverride.Caption = "Ungroup Tab";
                else if (e.DataView.Properties.TryGetValue("SourceManager", out var sm) && sm != TabManager)
                    e.DragUIOverride.Caption = "Move Tab to Window";
                else
                    e.DragUIOverride.Caption = "Reorder Tabs";
            }
            else if (type == "TabGroup")
            {
                if (e.DataView.Properties.TryGetValue("SourceManager", out var sm) && sm != TabManager)
                    e.DragUIOverride.Caption = "Move Group Tabs to Window";
                else
                    e.DragUIOverride.Caption = "Dissolve Group";
            }
        }
    }

    private void TabsList_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Properties.TryGetValue("DragType", out var typeObj) || typeObj is not string type) return;
        if (!e.DataView.Properties.TryGetValue("SourceManager", out var smObj) || smObj is not TabManager sourceManager) return;
        if (!e.DataView.Properties.TryGetValue("DragItem", out var item)) return;

        if (TabManager?.Instance.Name != sourceManager.Instance.Name) return;

        MoveTipsVisible = Visibility.Collapsed;

        var isSameWindow = sourceManager == TabManager;

        // Calculate Drop Index
        var listView = sender as ListView;
        var position = e.GetPosition(listView);
        var targetIndex = TabManager?.Tabs.Count ?? 0;
        
        if (listView?.Items != null && TabManager != null)
        {
            for (int i = 0; i < listView.Items.Count; i++)
            {
                var container = listView.ContainerFromIndex(i) as ListViewItem;
                if (container != null)
                {
                    var bounds = container.TransformToVisual(listView).TransformBounds(
                        new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                    
                    if (position.Y < bounds.Top + bounds.Height / 2)
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }
        }

        if (type == "Tab" && item is WebviewTab tab)
        {
            if (isSameWindow)
            {
                // If it has a SourceGroupId, it's coming from a group -> Ungroup it
                if (e.DataView.Properties.ContainsKey("SourceGroupId"))
                {
                    TabManager?.MoveTabToGroup(tab.Id, -1, targetIndex);
                }
                // Else: It's already in this list, ListView handles reordering automatically
            }
            else
            {
                TabManager.MoveTabFromWindow(tab, sourceManager, targetIndex);
            }
        }
        else if (type == "TabGroup" && item is TabGroup group)
        {
            if (isSameWindow)
            {
                TabManager.DissolveGroup(group, targetIndex);
            }
            else
            {
                TabManager.MoveGroupTabsFromWindow(group, sourceManager, targetIndex);
            }
        }
    }
    
    private void TabsList_DragEnter(object sender, DragEventArgs e)
    {
        e.Handled = false;
        if (e.DataView.Properties.ContainsKey("DragItem"))
        {
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
        }
    }

    private void GroupsList_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("SourceManager", out var smObj) && smObj is TabManager sourceManager)
        {
            if (TabManager?.Instance.Name != sourceManager.Instance.Name)
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }
        }

        if (e.DataView.Properties.TryGetValue("DragType", out object dragType))
        {
            if (dragType.ToString() == "TabGroup")
            {
                if (e.DataView.Properties.TryGetValue("SourceManager", out var sm) && sm != TabManager)
                    e.DragUIOverride.Caption = "Move Group to Window";
                else
                    e.DragUIOverride.Caption = "Reorder groups";
                
                e.AcceptedOperation = DataPackageOperation.Move;
            }
            else if (dragType.ToString() == "Tab")
            {
                // Don't handle at ListView level - let it bubble to NewTabGroupCard
                // Only set AcceptedOperation if we're not over a group card
                var element = e.OriginalSource as FrameworkElement;
                
                // Walk up the visual tree to see if we're over a NewTabGroupCard
                while (element != null)
                {
                    if (element is NewTabGroupCard)
                    {
                        // We're over a group card - don't handle here
                        e.Handled = false;
                        return;
                    }
                    element = element.Parent as FrameworkElement;
                }
                
                // Not over a group card, could be creating a new group
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.Caption = "Create new group";
            }
        }
    }

    private void GroupsList_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Properties.TryGetValue("DragType", out object dragType)) return;
        if (!e.DataView.Properties.TryGetValue("SourceManager", out var smObj) || smObj is not TabManager sourceManager) return;
        
        if (TabManager?.Instance.Name != sourceManager.Instance.Name) return;

        MoveTipsVisible = Visibility.Collapsed;

        var isSameWindow = sourceManager == TabManager;

            if (dragType.ToString() == "TabGroup")
            {
                if (isSameWindow)
                {
                    // Handle group reordering (ListView handles this automatically)
                }
                else
                {
                    TabManager.MoveGroupFromWindow(e.DataView.Properties["DragItem"] as TabGroup, sourceManager);
                }
                return;
            }
            else if (dragType.ToString() == "Tab" && e.DataView.Properties.TryGetValue("DragItem", out object tabObj))
            {
                var element = e.OriginalSource as FrameworkElement;
                
                // Walk up the visual tree to see if we're over a NewTabGroupCard
                while (element != null)
                {
                    if (element is NewTabGroupCard groupCard)
                    {
                        // We're over a group card - don't handle here, let NewTabGroupCard handle it
                        e.Handled = false;
                        return;
                    }
                    element = element.Parent as FrameworkElement;
                }
                
                // Dropped in empty space between groups - create new group
                var tab = tabObj as WebviewTab;
                if (tab != null && TabManager != null)
                {
                    if (isSameWindow)
                    {
                        var newGroup = TabManager.CreateGroup();
                        TabManager.MoveTabToGroup(tab.Id, newGroup.Id);
                    }
                    else
                    {
                        TabManager.MoveTabFromWindowToNewGroup(tab, sourceManager);
                    }
                }
            }
    }

    private void GroupsList_DragEnter(object sender, DragEventArgs e)
    {
        // Remove this handler or make it similar to DragOver
        // The individual NewTabGroupCard controls handle their own DragEnter
        e.Handled = false;
    }


    private void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WebviewTab wt)
        {
            TabManager?.SwapActiveTabTo(wt.Id);
        }
    }

    private void UIElement_OnDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("DragType", out var type) &&
            e.DataView.Properties.TryGetValue("SourceManager", out var smObj) && 
            smObj is TabManager sourceManager)
        {
            if (TabManager?.Instance.Name != sourceManager.Instance.Name)
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }

            e.AcceptedOperation = DataPackageOperation.Move;
            MoveTipsVisible = Visibility.Visible;
            
            if (type.ToString() == "Tab")
            {
                DropAreaText.Text = "+ New Tab Group";
                e.DragUIOverride.Caption = "Create new group";
            }
            else if (type.ToString() == "TabGroup")
            {
                if (sourceManager != TabManager)
                {
                    DropAreaText.Text = "Move Group Here";
                    e.DragUIOverride.Caption = "Move Group Here";
                }
                else
                {
                    DropAreaText.Text = "Dissolve Group";
                    e.DragUIOverride.Caption = "Dissolve Group";
                }
            }
        }
    }

    private void UIElement_OnDragEnter(object sender, DragEventArgs e)
    {
        e.Handled = false;
        if (e.DataView.Properties.ContainsKey("DragItem"))
        {
            MoveTipsVisible = Visibility.Visible;
        }
    }

    private void UIElement_OnDrop(object sender, DragEventArgs e)
    {
        MoveTipsVisible = Visibility.Collapsed;
        
        // This handles the specific "Drop Area" at the bottom of the list
        if (e.DataView.Properties.TryGetValue("DragItem", out var item) &&
            e.DataView.Properties.TryGetValue("DragType", out var type) &&
            e.DataView.Properties.TryGetValue("SourceManager", out var smObj) && 
            smObj is TabManager sourceManager)
        {
            if (TabManager?.Instance.Name != sourceManager.Instance.Name) return;

            var isSameWindow = sourceManager == TabManager;

            if (type.ToString() == "Tab" && item is WebviewTab tab)
            {
                // Logic for dropping a tab on the "New Group" area
                if (isSameWindow)
                {
                    var newGroup = TabManager?.CreateGroup();
                    if (newGroup != null)
                    {
                        TabManager?.MoveTabToGroup(tab.Id, newGroup.Id);
                    }
                }
                else
                {
                    TabManager?.MoveTabFromWindowToNewGroup(tab, sourceManager);
                }
            }
            else if (type.ToString() == "TabGroup" && item is TabGroup group)
            {
                if (isSameWindow)
                    TabManager.DissolveGroup(group);
                else
                    TabManager.MoveGroupFromWindow(group, sourceManager);
            }
        }
    }


    private void Root_OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("SourceManager", out var smObj) && smObj is TabManager sourceManager)
        {
            if (TabManager?.Instance.Name != sourceManager.Instance.Name) return;
        }

        OpenSideBar();
        MoveTipsVisible = Visibility.Visible;
    }

    private void Root_OnDragLeave(object sender, DragEventArgs e)
    {
        CloseSideBar();
        MoveTipsVisible = Visibility.Collapsed;
    }
}
