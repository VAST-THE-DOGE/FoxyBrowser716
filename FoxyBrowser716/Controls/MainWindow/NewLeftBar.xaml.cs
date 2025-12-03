using System;
using System.Collections.Generic;
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
    }

    
    [ObservableProperty] internal partial Theme CurrentTheme
    {
        get;
        set;
    } = DefaultThemes.LightMode;

    private void ApplyTheme()
    {
        //TODO get all these applied
        Root.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColor);
        Root.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        
        Div.Background = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);

        HomeCard.CurrentTheme = CurrentTheme;
        PinCard.CurrentTheme = CurrentTheme;
        BookmarkCard.CurrentTheme = CurrentTheme;
        
        /*foreach (var card in Tabs.Children)
            switch (card)
            {
                case TabGroupCard groupCard:
                    groupCard.CurrentTheme = CurrentTheme;
                    break;
                case TabCard tabCard:
                    tabCard.CurrentTheme = CurrentTheme;
                    break;
            }*/
        // foreach (var card in Pins.Children)
        //     if (card is TabCard tabCard)
        //         tabCard.CurrentTheme = CurrentTheme;
    }

    #region SidebarAnimator
    public bool LockSideBar { get; set; }
    
    internal async void OpenSideBar()
    {
        if (SideOpen) return;

        SideOpen = true;
        
        await AnimationBuilder.Create()
            .Size(Axis.X, 260, null, null, TimeSpan.FromSeconds(0.2), null, EasingType.Quintic, EasingMode.EaseOut, FrameworkLayer.Xaml)
            .StartAsync(Root);
    }

    internal async void CloseSideBar()
    {
        if (!SideOpen) return;

        SideOpen = false;
        
        await AnimationBuilder.Create()
            .Size(Axis.X, 30, null, null, TimeSpan.FromSeconds(0.2), null, EasingType.Quintic, EasingMode.EaseIn, FrameworkLayer.Xaml)
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

    private void NewTabCard_OnOnDragStarted(NewTabCard obj, ManipulationStartedRoutedEventArgs arg)
    {
        Debug.WriteLine("Drag started");
    }

    private void NewTabCard_OnOnDragCompleted(NewTabCard obj, ManipulationCompletedRoutedEventArgs arg)
    {
        Debug.WriteLine("Drag Moved");

    }

    private void NewTabCard_OnOnDragMoved(NewTabCard obj, ManipulationDeltaRoutedEventArgs arg)
    {
        Debug.WriteLine("Drag Completed");

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
}
