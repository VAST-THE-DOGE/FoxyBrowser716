using FoxyBrowser716_WinUI.DataManagement;
using FoxyBrowser716_WinUI.DataObjects.Complex;
using Material.Icons;
using Material.Icons.WinUI3;

namespace FoxyBrowser716_WinUI.Controls.MainWindow;

public sealed partial class InstanceCard : UserControl
{
    public event Action? OpenRequested;
    public event Action? TransferRequested;
    
    public InstanceCard()
    {
        InitializeComponent();
        ApplyTheme();
    }

    public InstanceCard(Instance instance, bool isCurrent) : this()
    {
        InstanceLabel.Text = instance.Name;
        
        if (instance.IsPrimaryInstance)
            Icon.Child = new MaterialIcon { Kind = MaterialIconKind.Star };
        
        if (isCurrent)
        {
            ButtonOpen.Visibility = Visibility.Collapsed;
            ButtonTransfer.Visibility = Visibility.Collapsed;
            CurrentLabel.Visibility = Visibility.Visible;
        }
    }
    
    internal Theme CurrentTheme { get; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        Root.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColor);
        Root.Background = new SolidColorBrush(MouseOver ? CurrentTheme.PrimaryAccentColorSlightTransparent : CurrentTheme.PrimaryBackgroundColorVeryTransparent);

        if (Icon.Child is FrameworkElement iconElement)
        {
            iconElement.SetValue(ForegroundProperty, new SolidColorBrush(CurrentTheme.PrimaryHighlightColor));
        }
        
        InstanceLabel.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        CurrentLabel.Foreground = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);

        ButtonOpen.CurrentTheme = CurrentTheme;
        ButtonTransfer.CurrentTheme = CurrentTheme;
    }

    
    private bool MouseOver;
    /*private void Root_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = true;
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryAccentColorSlightTransparent);
    }

    private void Root_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = false;
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryBackgroundColorVeryTransparent);
    }*/
    
    /*
    private void Root_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (ButtonClose.PointerOver) return;
        
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryBackgroundColorVeryTransparent, 0.3);
        OnClick?.Invoke();
    }
    */
    
    /*private void Root_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (ButtonClose.PointerOver) return;

        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryHighlightColorSlightTransparent, 0.05);
    }*/

    private void ButtonOpen_OnOnClick(object sender, RoutedEventArgs e)
    {
        OpenRequested?.Invoke();
    }

    private void ButtonTransfer_OnOnClick(object sender, RoutedEventArgs e)
    {
        TransferRequested?.Invoke();
    }
}