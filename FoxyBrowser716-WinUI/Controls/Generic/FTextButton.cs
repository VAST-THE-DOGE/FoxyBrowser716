using Windows.UI.Text;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using Microsoft.UI.Text;

namespace FoxyBrowser716_WinUI.Controls.Generic;

public sealed partial class FTextButton : ContentControl
{
    private Border _border;
    private TextBlock _textBlock;
    
    public event RoutedEventHandler? OnClick;
    
    public static readonly DependencyProperty ButtonTextProperty = DependencyProperty.Register(
        nameof(ButtonText), typeof(string), typeof(FTextButton),
        new PropertyMetadata(null, ButtonTextChanged));
    
    public string ButtonText
    {
        get => (string)GetValue(ButtonTextProperty);
        set { SetValue(ButtonTextProperty, value);
            ButtonTextChanged(this, null);
        }
    }

    private static void ButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs? e)
    {
        var control = (FTextButton)d;
        control._textBlock.Text = control.ButtonText;
    }
    
    public static readonly DependencyProperty ForceHighlightProperty = DependencyProperty.Register(
        nameof(ForceHighlight), typeof(bool), typeof(FTextButton),
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
        var control = (FTextButton)d;
        control._border.Background = new SolidColorBrush(control.ForceHighlight 
            ? control._currentTheme.PrimaryHighlightColor 
            : control.PointerOver 
                ? control.CurrentTheme.PrimaryAccentColor 
                : control.CurrentTheme.PrimaryAccentColorSlightTransparent);
        control._border.BorderBrush = new SolidColorBrush(control.ForceHighlight 
            ? control._currentTheme.SecondaryHighlightColor 
            : control.PointerOver 
                ? control.CurrentTheme.SecondaryAccentColor 
                : control.CurrentTheme.SecondaryAccentColorSlightTransparent);
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
        if (ForceHighlight)
        {
            _border.Background = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);
            _border.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryHighlightColor);
        }
        else
        {
            _border.Background = new SolidColorBrush(PointerOver ? CurrentTheme.PrimaryAccentColor : CurrentTheme.PrimaryAccentColorSlightTransparent);
            _border.BorderBrush = new SolidColorBrush(PointerOver ? CurrentTheme.SecondaryAccentColor : CurrentTheme.SecondaryAccentColorSlightTransparent);
        }

        _textBlock.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
    }
    
    public bool PointerOver { get; set; }
    
    public FTextButton()
    {
        DefaultStyleKey = typeof(FTextButton);

        _textBlock = new TextBlock
        {
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
        };
        
        _border = new Border
        {
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10),
            Child = _textBlock,
        };

        Content = _border;
        
        
        _border.PointerEntered += (_,_) =>
        {
            PointerOver = true;
            
            if (ForceHighlight) return;

            ChangeColorAnimation(_border.Background, CurrentTheme.PrimaryAccentColor);
            ChangeColorAnimation(_border.BorderBrush, CurrentTheme.PrimaryHighlightColor);
        };

        _border.PointerExited += (_, _) =>
        {
            PointerOver = false;
            
            if (ForceHighlight) return;
            
            ChangeColorAnimation(_border.Background, CurrentTheme.PrimaryAccentColorSlightTransparent);
            ChangeColorAnimation(_border.BorderBrush, CurrentTheme.SecondaryAccentColorSlightTransparent);
        };
        
        _border.PointerPressed += (_, _) =>
        {
            if (ForceHighlight) return;
            
            ChangeColorAnimation(_border.Background, CurrentTheme.PrimaryHighlightColor, 0.05);
            ChangeColorAnimation(_border.BorderBrush, CurrentTheme.SecondaryHighlightColor, 0.05);
        };

        _border.PointerReleased += (_, _) =>
        {
            ChangeColorAnimation(_border.Background, CurrentTheme.PrimaryAccentColor, 0.3);
            ChangeColorAnimation(_border.BorderBrush, CurrentTheme.PrimaryHighlightColor, 0.3);
            
            OnClick?.Invoke(this, new RoutedEventArgs());
        };

        ApplyTheme();
    }
}
