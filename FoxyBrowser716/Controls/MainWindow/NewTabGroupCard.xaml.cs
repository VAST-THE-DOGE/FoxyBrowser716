using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using FoxyBrowser716.Controls.Generic;
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
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716.Controls.MainWindow;

[ObservableObject]
public sealed partial class NewTabGroupCard : UserControl
{
    public bool IsCollapsed { get; private set; }
    
    [ObservableProperty] public partial TabGroup TabGroup { get; set; }
    
    public NewTabGroupCard()
    {
        InitializeComponent();
    }
    
    [ObservableProperty] public partial Theme CurrentTheme { get; set; } = DefaultThemes.DarkMode;
    
    private void ButtonCollapse_OnOnClick(object sender, RoutedEventArgs e)
    {
        SetCollapsed(!IsCollapsed);
    }

    public void SetCollapsed(bool isCollapsed)
    {
        IsCollapsed = isCollapsed;
        TabHolder.Visibility = IsCollapsed ? Visibility.Collapsed : Visibility.Visible;
        if (ButtonCollapse.Content is MaterialIcon icon)
            icon.Kind = IsCollapsed ? MaterialIconKind.ExpandMore : MaterialIconKind.ExpandLess;
    }
    
    private void TabCard_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // ButtonCollapse.Visibility = e.NewSize.Width > Icon.Width + ButtonCollapse.Width ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TabCard_OnClick(NewTabCard obj)
    {
        if (obj.Tag is WebviewTab wt)
            TabGroup.TabManager.SwapActiveTabTo(wt.Id);
    }

    private void TabCard_OnCloseRequested(NewTabCard obj)
    {
        if (obj.Tag is WebviewTab wt)
            TabGroup.TabManager.RemoveTab(wt.Id);
    }

    private async void TabCard_OnDuplicateRequested(NewTabCard obj)
    {
        if (obj.Tag is WebviewTab wt)
        {
            var id = TabGroup.TabManager.AddTab(wt.Info.Url);
            TabGroup.TabManager.MoveTabToGroup(id,TabGroup.Id);
            TabGroup.TabManager.SwapActiveTabTo(id);
        }
    }

    private void NewTabCard_OnOnDragStarted(NewTabCard arg1, ManipulationStartedRoutedEventArgs arg2)
    {
    }

    private void NewTabCard_OnOnDragMoved(NewTabCard arg1, ManipulationDeltaRoutedEventArgs arg2)
    {
    }

    private void NewTabCard_OnOnDragCompleted(NewTabCard arg1, ManipulationCompletedRoutedEventArgs arg2)
    {
    }
}