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

namespace FoxyBrowser716.Controls.Generic;

public sealed partial class FTextInput : UserControl
{
    public static readonly DependencyProperty PlaceHolderTextProperty = DependencyProperty.Register(
        nameof(PlaceHolderText), typeof(string), typeof(FTextInput),
        new PropertyMetadata(string.Empty, PlaceHolderTextChanged));
    
    public string PlaceHolderText
    {
        get => (string)GetValue(PlaceHolderTextProperty);
        set { SetValue(PlaceHolderTextProperty, value);
            PlaceHolderTextChanged(this, null);
        }
    }
    
    public static readonly DependencyProperty InputMiddlewareProperty = DependencyProperty.Register(
        nameof(InputMiddleware), typeof(Func<string, string, string>), typeof(FTextInput),
        new PropertyMetadata(null));

    public Func<string, string, string>? InputMiddleware
    {
        get => (Func<string, string, string>?)GetValue(InputMiddlewareProperty);
        set => SetValue(InputMiddlewareProperty, value);
    }


    private static void PlaceHolderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs? e)
    {
        var control = (FTextInput)d;
        control.SearchBox.PlaceholderText = control.PlaceHolderText;
    }

    public void SetText(string text)
    {
        SearchBox.Text = text;
    }
    public string GetText()
    {
        return SearchBox.Text;
    }
    
    public event Action<string>? OnTextChanged;
    public event Action? EnterPressed;
    
    public FTextInput()
    {
        InitializeComponent();
    }
    
    public FTextInput(string CurrentText)
    {
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(CurrentText))
            SearchBox.Text = CurrentText;
    }
    
    internal Theme CurrentTheme
    { get => field; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        SearchBox.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryAccentColorSlightTransparent);
        SearchBox.Background = new SolidColorBrush(CurrentTheme.PrimaryAccentColorSlightTransparent);
        SearchBox.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        SearchBox.PlaceholderForeground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor);
        SearchBox.SelectionHighlightColor = new SolidColorBrush(CurrentTheme.SecondaryHighlightColor);
        SearchBox.SelectionHighlightColorWhenNotFocused = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);
    }

    private void SearchBox_OnGotFocus(object sender, RoutedEventArgs e)
    {
        ChangeColorAnimation(SearchBox.BorderBrush, CurrentTheme.PrimaryHighlightColor);
        ChangeColorAnimation(SearchBox.Background, CurrentTheme.PrimaryBackgroundColorSlightTransparent);
    }

    private void SearchBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        ChangeColorAnimation(SearchBox.BorderBrush, CurrentTheme.SecondaryAccentColorSlightTransparent);
        ChangeColorAnimation(SearchBox.Background, CurrentTheme.PrimaryAccentColorSlightTransparent);
    }

    private void SearchBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        OnTextChanged?.Invoke(SearchBox.Text);
        
        if (e.Key == Windows.System.VirtualKey.Enter)
            EnterPressed?.Invoke();
    }
    
    private void SearchBox_TextChanging(object sender, TextBoxBeforeTextChangingEventArgs e)
    {
        if (InputMiddleware is null) return; // let the text change like normal
        
        // call the middleware to see what to set the text to
        var newText = InputMiddleware(SearchBox.Text, e.NewText);
        
        if (newText == e.NewText) return; // let the text change like normal. it is valid.
        
        e.Cancel = true;
        SearchBox.Text = newText;
    }
}
