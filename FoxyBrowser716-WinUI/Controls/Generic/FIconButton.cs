using FoxyBrowser716_WinUI.DataObjects.Basic;

namespace FoxyBrowser716_WinUI.Controls.Generic;

public sealed partial class FIconButton : ContentControl
{
    public event RoutedEventHandler? OnClick;
    
    public static readonly DependencyProperty ForceHighlightProperty = DependencyProperty.Register(
        nameof(ForceHighlight), typeof(bool), typeof(FIconButton),
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
        var control = (FIconButton)d;
        control.Background = new SolidColorBrush(control.ForceHighlight 
            ? control.CurrentTheme.PrimaryHighlightColorSlightTransparent
            : control.PointerOver ? control.CurrentTheme.PrimaryAccentColorSlightTransparent : Colors.Transparent);
    }
    
    internal Theme CurrentTheme { get; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        if (ForceHighlight)
        {
            Background = new SolidColorBrush(CurrentTheme.PrimaryHighlightColorSlightTransparent);
        }
        else
        {
            Background = new SolidColorBrush(PointerOver ? CurrentTheme.PrimaryAccentColorSlightTransparent : Colors.Transparent);;
        }
        Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
    }
    
    internal bool PointerOver { get; set; }
    
    public FIconButton()
    {
        DefaultStyleKey = typeof(FIconButton);
        
        PointerEntered += (_,_) =>
        {
            PointerOver = true;
            
            if (ForceHighlight) return;

            ChangeColorAnimation(Background, CurrentTheme.PrimaryAccentColorSlightTransparent);
        };

        PointerExited += (_, _) =>
        {
            PointerOver = false;
            
            if (ForceHighlight) return;
            
            ChangeColorAnimation(Background, Colors.Transparent);
        };
        
        PointerPressed += (_, _) =>
        {
            if (ForceHighlight) return;
            
            ChangeColorAnimation(Background, CurrentTheme.PrimaryHighlightColorSlightTransparent, 0.05);
        };

        PointerReleased += (_, _) =>
        {
            ChangeColorAnimation(Background, CurrentTheme.PrimaryAccentColorSlightTransparent, 0.3);
            
            OnClick?.Invoke(this, new RoutedEventArgs());
        };

        ApplyTheme();
    }
}
