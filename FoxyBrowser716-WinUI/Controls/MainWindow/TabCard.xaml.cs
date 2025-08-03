using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using FoxyBrowser716_WinUI.Controls.Generic;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using FoxyBrowser716_WinUI.DataObjects.Complex;
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

namespace FoxyBrowser716_WinUI.Controls.MainWindow;

public sealed partial class TabCard : UserControl
{
    public event Action<int>? CloseRequested;
    public event Action<int>? DuplicateRequested;
    public event Action<int>? OnClick;

    private DateTime? _lastDuplication;
    
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(
        nameof(Id), typeof(int), typeof(TabCard),
        new PropertyMetadata(0));

    public int Id
    {
        get => (int)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }
    
    public static readonly DependencyProperty ForceHighlightProperty = DependencyProperty.Register(
        nameof(ForceHighlight), typeof(bool), typeof(TabCard),
        new PropertyMetadata(null, ForceHighlightChanged));
    
    public bool ForceHighlight
    {
        get => (bool)GetValue(ForceHighlightProperty);
        set { SetValue(ForceHighlightProperty, value);
            ForceHighlightChanged(this, null);
        }
    }
    
    private static void ForceHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs? e)
    {
        var control = (TabCard)d;
        ChangeColorAnimation(control.Root.BorderBrush,
            control.ForceHighlight
                ? control.CurrentTheme.PrimaryHighlightColor
                : control.CurrentTheme.SecondaryBackgroundColor);
    }
    
    public static readonly DependencyProperty ShowDuplicateProperty = DependencyProperty.Register(
        nameof(ShowDuplicate), typeof(bool), typeof(TabCard),
        new PropertyMetadata(null, ShowDuplicateChanged));
    
    public bool ShowDuplicate
    {
        get => (bool)GetValue(ShowDuplicateProperty);
        set { SetValue(ShowDuplicateProperty, value);
            ShowDuplicateChanged(this, null);
        }
    }
    
    private static void ShowDuplicateChanged(DependencyObject d, DependencyPropertyChangedEventArgs? e)
    {
        var control = (TabCard)d;
        control.ButtonDuplicate.Visibility = control.ShowDuplicate ? Visibility.Visible : Visibility.Collapsed;
    }
    
    public static readonly DependencyProperty ShowCloseProperty = DependencyProperty.Register(
        nameof(ShowClose), typeof(bool), typeof(TabCard),
        new PropertyMetadata(null, ShowCloseChanged));
    
    public bool ShowClose
    {
        get => (bool)GetValue(ShowCloseProperty);
        set { SetValue(ShowCloseProperty, value);
            ShowCloseChanged(this, null);
        }
    }
    
    private static void ShowCloseChanged(DependencyObject d, DependencyPropertyChangedEventArgs? e)
    {
        var control = (TabCard)d;
        control.ButtonClose.Visibility = control.ShowClose ? Visibility.Visible : Visibility.Collapsed;
    }


    
    public TabCard()
    {
        InitializeComponent();
        ApplyTheme();
    }

    public TabCard(WebviewTab tab) : this()
    {
        Id = tab.Id;
        tab.Info.PropertyChanged += (_, _) => RefreshData(tab);
        RefreshData(tab);
    }

    public TabCard(int id, WebsiteInfo websiteInfo) : this()
    {
        Id = id;
        websiteInfo.PropertyChanged += (_, _) => RefreshData(websiteInfo);
        RefreshData(websiteInfo);
    }

    private void RefreshData(WebviewTab tab)
    {
        Icon.Child = new Image
        {
            Source = new BitmapImage(string.IsNullOrWhiteSpace(tab.Info.FavIconUrl) ? new Uri("https://TODO") : new Uri(tab.Info.FavIconUrl)), //TODO
            Width = 18, Height = 18,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        Label.Text = tab.Info.Title;
    }

    private void RefreshData(WebsiteInfo websiteInfo)
    {
        Icon.Child = new Image
        {
            Source = new BitmapImage(string.IsNullOrWhiteSpace(websiteInfo.FavIconUrl) ? new Uri("https://TODO") : new Uri(websiteInfo.FavIconUrl)), //TODO
            Width = 18, Height = 18,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        Label.Text = websiteInfo.Title;
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
        Root.BorderBrush = new SolidColorBrush(ForceHighlight ? CurrentTheme.PrimaryHighlightColor : CurrentTheme.SecondaryBackgroundColor);
        Root.Background = new SolidColorBrush(MouseOver ? CurrentTheme.PrimaryAccentColorSlightTransparent : CurrentTheme.PrimaryBackgroundColorVeryTransparent);

        if (Icon.Child is MaterialControlIcon mi)
        {
            mi.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        }
        Label.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);

        ButtonClose.CurrentTheme = CurrentTheme with{ PrimaryAccentColor = CurrentTheme.NoColor};
        ButtonDuplicate.CurrentTheme = CurrentTheme with{ PrimaryAccentColor = Colors.DodgerBlue};
    }

    
    private bool MouseOver;
    private void Root_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = true;
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryAccentColorSlightTransparent);
    }

    private void Root_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = false;
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryBackgroundColorVeryTransparent);
    }

    private void ButtonClose_OnOnClick(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(Id);
    }

    private void ButtonDuplicate_OnOnClick(object sender, RoutedEventArgs e)
    {
        _lastDuplication = DateTime.Now;
        DuplicateRequested?.Invoke(Id);
    }

    private void Root_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (ButtonDuplicate.PointerOver || ButtonClose.PointerOver) return;
        
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryBackgroundColorVeryTransparent, 0.3);
        OnClick?.Invoke(Id);
    }
    
    private void Root_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (ButtonDuplicate.PointerOver || ButtonClose.PointerOver) return;

        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryHighlightColorSlightTransparent, 0.05);
    }
}