using Windows.Foundation;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using Microsoft.UI.Xaml.Input;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716_WinUI.Controls.MainWindow;

public sealed partial class TopBar : UserControl
{
    public bool IsBorderless { get; private set; }
    public event Action? BorderlessToggled;
    
    public event Action? MenuClicked;
    
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
        SearchBox.SelectionHighlightColor = new SolidColorBrush(CurrentTheme.SecondaryHighlightColorVeryTransparent);
        SearchBox.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
    }
    
    public TopBar()
    {
        InitializeComponent();
        
        ApplyTheme();
    }

    private void BorderlessToggle_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleBorderless(!IsBorderless);
        
    }

    public void ToggleBorderless(bool isBorderless)
    {
        IsBorderless = isBorderless;
        
        BorderlessToggled?.Invoke();
        ButtonBorderlessToggle.ForceHighlight = IsBorderless;
        
        DragZone.Visibility = IsBorderless ? Visibility.Collapsed : Visibility.Visible;
        ButtonMaximize.Visibility = IsBorderless ? Visibility.Collapsed : Visibility.Visible;
        ButtonMinimize.Visibility = IsBorderless ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ButtonMenu_OnClick(object sender, RoutedEventArgs e)
    {
        MenuClicked?.Invoke();
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
        var searchText = SearchBox.Text.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            // should update later with a webview2 getting the focus, but doing so now can't hurt:
            SearchBackground.Focus(FocusState.Programmatic);
            
            SearchClicked?.Invoke(searchText);
        }
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

    private void SearchBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ButtonSearch_OnClick(this, e);
        }
    }

    private void SearchBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        ChangeColorAnimation(SearchBackground.BorderBrush, CurrentTheme.SecondaryAccentColorSlightTransparent);
        ChangeColorAnimation(SearchBackground.Background, CurrentTheme.PrimaryAccentColorSlightTransparent);

    }

    private void SearchBox_OnGotFocus(object sender, RoutedEventArgs e)
    {
        ChangeColorAnimation(SearchBackground.BorderBrush, CurrentTheme.PrimaryHighlightColor);
        ChangeColorAnimation(SearchBackground.Background, CurrentTheme.PrimaryBackgroundColorSlightTransparent);
    }

    public void UpdateSearchBar(bool showRefresh, bool showBack, bool showForward, string? searchText=null)
    {
        ButtonRefresh.Visibility = showRefresh ? Visibility.Visible : Visibility.Collapsed;
        ButtonBack.Visibility = showBack ? Visibility.Visible : Visibility.Collapsed;
        ButtonForward.Visibility = showForward ? Visibility.Visible : Visibility.Collapsed;

        if (searchText is { } text)
            SearchBox.Text = text;
    }

    public double GetSearchEngineOffset()
    {
        var transform = ButtonEngine.TransformToVisual(null);
        return transform.TransformPoint(new Point(0, 0)).X;
    }
}
