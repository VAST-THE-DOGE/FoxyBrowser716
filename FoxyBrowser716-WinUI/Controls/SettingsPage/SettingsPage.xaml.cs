using System;
using System.Collections.Generic;
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
    
    internal Theme CurrentTheme
    {
        get;
        set
        {
            field = value;
            ApplyTheme();
        }
    } = DefaultThemes.VastSea;

    private void ApplyTheme()
    {
        
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
            };
            
            CategoryViewer.Children.Add(categoryButton);
            
            SettingsViewer.Children.Add(new HeaderSetting(pair.Key.ToString()).GetEditor(_mainWindow));
            
            foreach (var control in pair.Value)
            {
                SettingsViewer.Children.Add(control.GetEditor(_mainWindow));
            }
            
            SettingsViewer.Children.Add(new DividerSetting().GetEditor(_mainWindow));
        }
    }
}
