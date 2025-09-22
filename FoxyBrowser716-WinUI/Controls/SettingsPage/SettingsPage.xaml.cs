using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using FoxyBrowser716_WinUI.Controls.Generic;
using FoxyBrowser716_WinUI.DataObjects.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716_WinUI.Controls.SettingsPage;

public sealed partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }
    
    internal Theme CurrentTheme { get; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        _settingsControls.ForEach(c => c.CurrentTheme = CurrentTheme);
        _categoryButtons.ForEach(c => c.CurrentTheme = CurrentTheme);
        
        InputSearch.CurrentTheme = CurrentTheme;
        ResultBlock.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);

        BorderSearch.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        BorderSearch.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);
        
        BorderCategories.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        BorderCategories.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);
        
        //TODO: temp
        var newGradient = new RadialGradientBrush()
        {
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            Center = new Point(0,1),
            GradientOrigin = new Point(0,1),
        };
        
        newGradient.GradientStops.Add(new GradientStop() { Color = CurrentTheme.PrimaryHighlightColor, Offset = 0 });
        newGradient.GradientStops.Add(new GradientStop() { Color = CurrentTheme.PrimaryBackgroundColor, Offset = 0.75 });
        RootGrid.Background = newGradient;
        
        //TODO: why is this not updating???
        // BackgroundPrimary.Color = CurrentTheme.PrimaryBackgroundColor;
        // BackgroundSecondary.Color = CurrentTheme.PrimaryHighlightColor;
        // Debug.WriteLine(string.Join(" -> ", BackgroundGradient.GradientStops.Select(gs => gs.Color)));
        // RootGrid.Background = new SolidColorBrush(Colors.Black);
        
        // BackgroundGradient.InterpolationSpace = CompositionColorSpace.Auto;
    }
    
    private MainWindow.MainWindow _mainWindow;
    private List<FTextButton> _categoryButtons = [];
    private List<ThemedUserControl> _settingsControls = [];

    public async Task Initialize(MainWindow.MainWindow mainWindow)
    {
        _mainWindow = mainWindow;

        var controls = _mainWindow.Instance.Settings.GetSettingControls(_mainWindow);

        _mainWindow.Instance.Settings.PropertyChanged += (s, e) =>
        {
            ResultBlock.Text += $"{e.PropertyName}, ";
        };
        
        foreach (var pair in controls)
        {
            var categoryButton = new FTextButton
            {
                ButtonText = pair.Key.ToString(),
                CurrentTheme = CurrentTheme,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(2),
                //TODO custom click thing here
            };

            categoryButton.OnClick += (_,_) =>
            {
                //TODO
            };
            
            _categoryButtons.Add(categoryButton);
            CategoryViewer.Children.Add(categoryButton);
            
            var headset = new HeaderSetting(pair.Key.ToString()).GetEditor(_mainWindow);
            _settingsControls.Add(headset);
            headset.CurrentTheme = CurrentTheme;
            SettingsViewer.Children.Add(headset);
            
            foreach (var control in pair.Value)
            {
                var editor = control.GetEditor(_mainWindow);
                _settingsControls.Add(editor);
                editor.CurrentTheme = CurrentTheme;
                SettingsViewer.Children.Add(editor);
            }

            var divSet = new DividerSetting().GetEditor(_mainWindow);
            _settingsControls.Add(divSet);
            divSet.CurrentTheme = CurrentTheme;
            SettingsViewer.Children.Add(divSet);
        }
    }
}
