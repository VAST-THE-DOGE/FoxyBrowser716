

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716_WinUI.Controls.HomePage;

public sealed partial class WidgetEditOverlay : UserControl
{
    public Theme CurrentTheme { get; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;
    internal WidgetBase AttachedWidget { get; set; }

    private void ApplyTheme()
    {
        Root.BorderBrush = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);
        Root.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        ButtonSettings.CurrentTheme = CurrentTheme;
        ButtonRemove.CurrentTheme = CurrentTheme with { PrimaryForegroundColor = CurrentTheme.NoColor };
        //TODO: need normal icon button and between them depending on size.
    }

    public WidgetEditOverlay()
    {
        InitializeComponent();
    }
    
    public WidgetEditOverlay(WidgetBase widget)
    {
        InitializeComponent();
        AttachedWidget = widget;
        ApplyTheme();
    }

    private void ButtonSettings_OnOnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ButtonRemove_OnOnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void Border_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // throw new NotImplementedException();
    }

    private void Border_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        // throw new NotImplementedException();
    }

    private void Root_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void Root_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void Root_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        // throw new NotImplementedException();
    }
}
