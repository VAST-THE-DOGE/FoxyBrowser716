

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using FoxyBrowser716_WinUI.Controls.Generic;
using FoxyBrowser716_WinUI.DataManagement;
using FoxyBrowser716_WinUI.DataObjects.Settings;

namespace FoxyBrowser716_WinUI.Controls.SettingsPage.SettingsCustomControls;

public sealed partial class ExtensionsController : ThemedUserControl
{
    private MainWindow.MainWindow _mainWindow;
    
    public ExtensionsController()
    {
        InitializeComponent();
    }
    
    public ExtensionsController(MainWindow.MainWindow mainWindow)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        ExtensionManager.ExtensionsModified += s => { if (s == _mainWindow.Instance.Name) Refresh(); };
    }

    protected override void ApplyTheme()
    {
        
    }

    private void Refresh()
    {
        Root.Children.Clear();
        foreach (var extension in _mainWindow.Instance.GetSavedExtensions())
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5),
            };

            var text = new TextBlock
            {
                Text = extension.WebviewName,
            };

            var disable = new FTextButton
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                CornerRadius = new CornerRadius(5),
                ButtonText = extension.IsEnabled ? "Disable" : "Enable",
            }; //TODO: use webview extension popup for this and implement in extension manager

            var remove = new FTextButton
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                CornerRadius = new CornerRadius(5),
                ButtonText = "Remove",
            };

            remove.OnClick += async (_, _) =>
            {
                await _mainWindow.Instance.RemoveExtension(_mainWindow.ExtensionPopupWebview, extension.Id);
            };
            
            panel.Children.Add(text);
            panel.Children.Add(disable);
            panel.Children.Add(remove);
            
            Root.Children.Add(panel);
        }
    }
}
