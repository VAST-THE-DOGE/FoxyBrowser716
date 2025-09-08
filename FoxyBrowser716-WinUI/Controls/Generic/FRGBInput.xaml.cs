using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using static System.Byte;

namespace FoxyBrowser716_WinUI.Controls.Generic;

public sealed partial class FRGBInput : UserControl
{
    public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register(
        nameof(ShowAlpha), typeof(bool), typeof(FRGBInput),
        new PropertyMetadata(true, OnShowAlphaChanged));
    
    public bool ShowAlpha
    {
        get => (bool)GetValue(ShowAlphaProperty);
        set => SetValue(ShowAlphaProperty, value);
    }

    private static void OnShowAlphaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (FRGBInput)d;
        control.UpdateAlphaVisibility();
    }

    public event Action<Color>? OnValueChanged;
    
    public FRGBInput()
    {
        InitializeComponent();
        SetupEventHandlers();
        UpdateAlphaVisibility();
    }
    
    public FRGBInput(byte? a, byte r, byte g, byte b)
    {
        InitializeComponent();
        SetupEventHandlers();
        UpdateAlphaVisibility();
        SetColor(a, r, g, b);
    }

    private void SetupEventHandlers()
    {
        ABox.TextChanged += OnTextBoxChanged;
        RBox.TextChanged += OnTextBoxChanged;
        GBox.TextChanged += OnTextBoxChanged;
        BBox.TextChanged += OnTextBoxChanged;
        
        ABox.KeyUp += OnTextBoxKeyUp;
        RBox.KeyUp += OnTextBoxKeyUp;
        GBox.KeyUp += OnTextBoxKeyUp;
        BBox.KeyUp += OnTextBoxKeyUp;
        
        ABox.GotFocus += OnTextBoxGotFocus;
        RBox.GotFocus += OnTextBoxGotFocus;
        GBox.GotFocus += OnTextBoxGotFocus;
        BBox.GotFocus += OnTextBoxGotFocus;
        
        ABox.LostFocus += OnTextBoxLostFocus;
        RBox.LostFocus += OnTextBoxLostFocus;
        GBox.LostFocus += OnTextBoxLostFocus;
        BBox.LostFocus += OnTextBoxLostFocus;
    }

    private void UpdateAlphaVisibility()
    {
        ABox.Visibility = ShowAlpha ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetColor(byte? a, byte r, byte g, byte b)
    {
        if (a.HasValue)
            ABox.Text = a.Value.ToString();
        else
            ABox.Text = ShowAlpha ? "255" : "";
            
        RBox.Text = r == 0 ? "" : r.ToString();
        GBox.Text = g == 0 ? "" : g.ToString();
        BBox.Text = b == 0 ? "" : b.ToString();
        
        UpdateColorPreview();
    }

    public void SetColor(Color color)
    {
        SetColor(color.A, color.R, color.G, color.B);
    }

    public Color GetColor()
    {
        byte a = 255;

        if (ShowAlpha && TryParse(ABox.Text, out var alphaValue)) a = alphaValue;

        TryParse(RBox.Text, out var r);
        TryParse(GBox.Text, out var g);
        TryParse(BBox.Text, out var b);

        return Color.FromArgb(a, r, g, b);
    }

    private void UpdateColorPreview()
    {
        var color = GetColor();
        ColorPreview.CurrentTheme = CurrentTheme with { PrimaryHighlightColor = color };
    }
    
    internal Theme CurrentTheme
    { get => field; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        Root.Background = new SolidColorBrush(CurrentTheme.PrimaryAccentColorSlightTransparent);
        Root.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);

        ABox.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        RBox.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        GBox.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        BBox.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        
        ABox.PlaceholderForeground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor);
        RBox.PlaceholderForeground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor);
        GBox.PlaceholderForeground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor);
        BBox.PlaceholderForeground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor);
        
        Div1.Background = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);
        Div2.Background = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);
        Div3.Background = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);

        UpdateColorPreview();
    }

    private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryBackgroundColorSlightTransparent);
        ChangeColorAnimation(Root.BorderBrush, CurrentTheme.PrimaryHighlightColorSlightTransparent);

        Div1.Background = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);
        Div2.Background = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);
        Div3.Background = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryAccentColorSlightTransparent);
        ChangeColorAnimation(Root.BorderBrush, CurrentTheme.SecondaryAccentColorSlightTransparent);

        Div1.Background = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);
        Div2.Background = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);
        Div3.Background = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);
    }

    private void OnTextBoxChanged(object sender, TextChangedEventArgs e)
    {
        UpdateColorPreview();
        OnValueChanged?.Invoke(GetColor());
    }

    private void OnTextBoxKeyUp(object sender, KeyRoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        if (int.TryParse(textBox.Text, out var value))
        {
            if (value <= 0)
                textBox.Text = "";
            else if (value > 255)
                textBox.Text = "255";
            
            if (textBox.Text.Length > 3)
                textBox.Text = value.ToString();
        }
        else if (!string.IsNullOrEmpty(textBox.Text))
        {
            textBox.Text = "";
        }
    }

    private double previousHeight = 0;
    private void ColorPreview_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Math.Abs(previousHeight - e.NewSize.Height) < 0.5) return;
        previousHeight = e.NewSize.Height;
        ColorPreview.Width = previousHeight;
    }
}