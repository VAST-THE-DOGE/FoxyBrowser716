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

public sealed partial class TabGroupCard : UserControl
{
    public bool IsCollapsed { get; private set; }
    
    public TabGroupCard(MaterialIconKind icon, string label, bool isCollapsed=false)
    {
        InitializeComponent();

        Icon.Child = new MaterialIcon() { Kind = icon };
        Label.Text = label;
        
        SetCollapsed(isCollapsed);
        
        ApplyTheme();
    }
    
    internal Theme CurrentTheme { get; set { field = value; ApplyTheme(); }} = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        Root.Background = new SolidColorBrush(CurrentTheme.SecondaryHighlightColorSlightTransparent);

        if (Icon.Child is FrameworkElement iconElement)
        {
            iconElement.SetValue(Control.ForegroundProperty, new SolidColorBrush(CurrentTheme.PrimaryForegroundColor));
        }
        Label.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);

        ButtonCollapse.CurrentTheme = CurrentTheme;

        foreach (var c in TabHolder.Children)
        {
            if (c is TabCard tabCard)
                tabCard.CurrentTheme = CurrentTheme;
        }
    }
    
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

    public void AddTabCard(TabCard tabCard)
    {
        TabHolder.Children.Add(tabCard);
    }
    
    public void RemoveTabCard(TabCard tabCard)
    {
        TabHolder.Children.Remove(tabCard);
    }
    
    private void TabCard_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ButtonCollapse.Visibility = e.NewSize.Width > Icon.Width + ButtonCollapse.Width ? Visibility.Visible : Visibility.Collapsed;
    }
}