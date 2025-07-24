using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716_WinUI.Controls.MainWindow;

public sealed partial class TopBar : UserControl
{
    public bool IsBorderless { get; private set; }
    public event Action? BorderlessToggled;
    
    public event Action? MinimizeClicked;
    public event Action? MaximizeClicked;
    public event Action? CloseClicked;
    
    public event Action? BackClicked;
    public event Action? ForwardClicked;
    public event Action? RefreshClicked;
    public event Action<string>? SearchClicked;
    public event Action? EngineClicked;
    
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
        
        ButtonMenu.CurrentTheme = CurrentTheme;
        ButtonBorderlessToggle.CurrentTheme = CurrentTheme;
        ButtonMinimize.CurrentTheme = CurrentTheme;
        ButtonMaximize.CurrentTheme = CurrentTheme;
        ButtonClose.CurrentTheme = CurrentTheme with { PrimaryAccentColor = CurrentTheme.NoColor };
        ButtonBack.CurrentTheme = CurrentTheme;
        ButtonForward.CurrentTheme = CurrentTheme;
        ButtonRefresh.CurrentTheme = CurrentTheme;
        ButtonSearch.CurrentTheme = CurrentTheme;
        ButtonEngine.CurrentTheme = CurrentTheme;
        
        SearchBackground.Background = new SolidColorBrush(CurrentTheme.PrimaryAccentColorSlightTransparent);
        SearchBackground.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);
    }
    
    public TopBar()
    {
        InitializeComponent();
        
        ApplyTheme();
    }

    private void BorderlessToggle_OnClick(object sender, RoutedEventArgs e)
    {
        IsBorderless = !IsBorderless;
        BorderlessToggled?.Invoke();
        ButtonBorderlessToggle.ForceHighlight = IsBorderless;
    }

    private void ButtonMenu_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshClicked?.Invoke();
    }

    private void ButtonBack_OnClick(object sender, RoutedEventArgs e)
    {
        BackClicked?.Invoke();
    }

    private void ButtonForward_OnClick(object sender, RoutedEventArgs e)
    {
        ForwardClicked?.Invoke();
    }

    private void ButtonSearch_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ButtonEngine_OnClick(object sender, RoutedEventArgs e)
    {
        EngineClicked?.Invoke();
    }

    private void ButtonMinimize_OnClick(object sender, RoutedEventArgs e)
    {
        MinimizeClicked?.Invoke();
    }

    private void ButtonMaximize_OnClick(object sender, RoutedEventArgs e)
    {
        MaximizeClicked?.Invoke();
    }

    private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
    {
        CloseClicked?.Invoke();
    }
}
