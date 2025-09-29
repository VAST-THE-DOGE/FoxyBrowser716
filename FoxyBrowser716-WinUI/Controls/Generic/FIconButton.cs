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
            : Colors.Transparent);
    }
    
    public static readonly DependencyProperty RoundedProperty = DependencyProperty.Register(
        nameof(Rounded), typeof(bool), typeof(FIconButton),
        new PropertyMetadata(true, RoundedChanged));
    
    public bool Rounded
    {
        get => (bool)GetValue(RoundedProperty);
        set { SetValue(RoundedProperty, value);
            RoundedChanged(this, null);
        }
    }

    private static void RoundedChanged(DependencyObject d, DependencyPropertyChangedEventArgs? e)
    {
        ((FIconButton)d).CornerRadius = new CornerRadius(((FIconButton)d).Rounded ? (Math.Min(((FIconButton)d).ActualWidth, ((FIconButton)d).ActualHeight) / 2 ) : 0);
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
            Background = new SolidColorBrush(Colors.Transparent);;
        }
        Foreground = new SolidColorBrush(PointerOver ? CurrentTheme.PrimaryAccentColorSlightTransparent : CurrentTheme.PrimaryForegroundColor);
    }
    
    internal bool PointerOver { get; set; }
    
    public FIconButton()
    {
        DefaultStyleKey = typeof(FIconButton);

        SizeChanged += (_, _) =>
        {
            var minSize = Math.Min(ActualWidth, ActualHeight);
            CornerRadius = new CornerRadius(Rounded ? minSize / 2 : 0);
        };
        
        PointerEntered += (_,_) =>
        {
            PointerOver = true;
            
            ChangeColorAnimation(Foreground, CurrentTheme.SecondaryForegroundColor);
        };

        PointerExited += (_, _) =>
        {
            PointerOver = false;
            
            ChangeColorAnimation(Foreground, CurrentTheme.PrimaryForegroundColor);
        };
        
        PointerPressed += (_, _) =>
        {
            ChangeColorAnimation(Foreground, CurrentTheme.PrimaryHighlightColor, 0.05);
        };

        PointerReleased += (_, _) =>
        {
            OnClick?.Invoke(this, new RoutedEventArgs());
            
            ChangeColorAnimation(Foreground, PointerOver ? CurrentTheme.SecondaryForegroundColor : CurrentTheme.PrimaryForegroundColor, 0.3);
        };

        ApplyTheme();
    }
}
