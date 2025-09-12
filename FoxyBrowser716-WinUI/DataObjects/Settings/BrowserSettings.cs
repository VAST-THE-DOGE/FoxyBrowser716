using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataManagement;

namespace FoxyBrowser716_WinUI.DataObjects.Settings;

//TODO: separate out so that this file is only focused on values.
// Keep enums in a file.
// Keep Attribute in it's own file
// Keep base class in a file (for get settings function).

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SettingInfoAttribute : Attribute
{
    public string? Name { get; init; } // null = use field name
    public string? Description { get; init; } // null = "" as description
    public required SettingsCategory Category { get; init; } // not null
    
    // extra for specific controls
    public string[]? Options { get; init; }
    public FoxyFileManager.ItemType? ItemType { get; init; }
    public bool AllowWebsiteUris { get; init; } = false;
    //TODO
    
    public Func<MainWindow, ThemedUserControl>? ControlFactory { get; init; } // null = premade control
}

public enum SettingsCategory
{
    General,
    WebView2,
    Misc
}

public sealed class BrowserSettings : INotifyPropertyChanged
{
    public Dictionary<SettingsCategory, List<ISetting>> GetSettingControls(MainWindow mainWindow)
    {
        var controls = new Dictionary<SettingsCategory, List<ISetting>>();
        var type = typeof(BrowserSettings);
        var fields = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            var attributes = (SettingInfoAttribute[])field.GetCustomAttributes(typeof(SettingInfoAttribute), false);
            
            if (attributes.FirstOrDefault() is not { } attribute) continue;
            
            if (!controls.TryGetValue(attribute.Category, out var catControls))
            {
                catControls = new List<ISetting>();
                controls.Add(attribute.Category, catControls);
            }
            
            if (attribute.ControlFactory is not null)
                catControls.Add(new CustomControlSetting(attribute.Name ?? field.Name, attribute.Description ?? "", attribute.ControlFactory));
            else
                switch (field.GetValue(this))
                {
                    case bool b:
                        catControls.Add(new BoolSetting(attribute.Name ?? field.Name, attribute.Description ?? "", b, b1 => field.SetValue(this, b1)));
                        break;
                    case int i:
                        catControls.Add(new IntSetting(attribute.Name ?? field.Name, attribute.Description ?? "", i, i1 => field.SetValue(this, i1)));
                        break;
                    case decimal d:
                        catControls.Add(new DecimalSetting(attribute.Name ?? field.Name, attribute.Description ?? "", d, d1 => field.SetValue(this, d1)));
                        break;
                    case double d:
                        throw new NotImplementedException();
                        //TODO: need more params in the attribute for max,min and step
                        //controls.Add(new SliderSetting(attribute.Name ?? field.Name, attribute.Description ?? "", d, d1 => field.SetValue(this, d1)));
                        break;
                    case string s:
                        if (attribute.Options is { } comboOptions)
                            throw new NotImplementedException(); //TODO combo
                        else
                            catControls.Add(new StringSetting(attribute.Name ?? field.Name, attribute.Description ?? "", s, s1 => field.SetValue(this, s1)));
                        break;
                    case Color c:
                        catControls.Add(new ColorSetting(attribute.Name ?? field.Name, attribute.Description ?? "", c, c1 => field.SetValue(this, c1)));
                        break;
                    case Uri u:
                        throw new NotImplementedException();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Type '{field.PropertyType.Name}' from field '{field.Name}' is not supported.'");
                }
        }
        
        return controls;
    }

    #region General

    [SettingInfo(Category = SettingsCategory.General)]
    public string TestString { get; set => SetField(ref field, value); } = "default value here";

    //TODO: enum support would be nice here:
    // dictionary map of enum to human readable.
    // detect enum, grab all values as options.
    // [SettingInfo(Category = SettingsCategory.General, Options = ["0", "1", "2", "3"])]
    // public string TestCombo = "1";
    
    [SettingInfo(Category = SettingsCategory.General)]
    public bool TestBool { get; set => SetField(ref field, value); } = true;
    
    [SettingInfo(Category = SettingsCategory.General, Name = "Another Test Name")]
    public int TestInt { get; set => SetField(ref field, value); } = 42;
    #endregion
    
    #region WebView2
    [SettingInfo(Category = SettingsCategory.WebView2)]
    public string TestWebView2 { get; set => SetField(ref field, value); } = "default value here";
    
    [SettingInfo(Category = SettingsCategory.WebView2)]
    public string TestWebView3 { get; set => SetField(ref field, value); } = "default value here";
    #endregion
    
    #region Misc
    [SettingInfo(Category = SettingsCategory.Misc)]
    public string TestMisc { get; set => SetField(ref field, value); } = "default value here";
    
    #endregion

    //TODO: look more into this
    //TODO: webview2 and everything needs an ApplySettings function that will call at creation and non change of settings.
    //TODO maybe each category as a sub class?
    /* Necessary Settings:
     *
     * Simple:
     * - performance for webview2
     * - performance for my UI
     * - other webview2 specific settings
     * - ai assistant settings
     * - tracking prevention
     * - default zoom
     * - color theme (dark/light mode)
     * - startup mode: none, restore, config
     * - restore browser on close?
     * - download location
     * - show download menu on download start
     * - sleeping tabs and performance settings
     * - reset to defaults
     * - default search engine, last used or specific one
     * - default browser
     *
     * Complex:
     * Theme pick and editing
     * extension management
     * instance management
     * error viewer/reporter/log-exporter
     * startup config
     * cookies and permission management for websites
     *
     */
    //TODO: need default static setting to use if none.
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public partial class PerformanceSettings : ObservableObject
{
    
}