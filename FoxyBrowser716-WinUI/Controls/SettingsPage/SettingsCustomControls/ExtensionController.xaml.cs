

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
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5),
            };

            var text = new TextBlock
            {
                Text = extension.WebviewName,
            };

            var remove = new FTextButton
            {
                CornerRadius = new CornerRadius(5)
            };
            
            panel.Children.Add(text);
            panel.Children.Add(remove);
            
            Root.Children.Add(panel);
        }
    }
}
