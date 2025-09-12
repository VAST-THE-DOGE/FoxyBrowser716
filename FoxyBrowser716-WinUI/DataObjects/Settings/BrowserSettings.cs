/*using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataManagement;

namespace FoxyBrowser716_WinUI.DataObjects.Settings;

//TODO: separate out so that this file is only focused on values.
// Keep enums in a file.
// Keep Attribute in it's own file
// Keep base class in a file (for get settings function).

[AttributeUsage(AttributeTargets.Field)]
public class SettingInfoAttribute(
    SettingsCategory category,
    string? name = null,
    string? description = null,
    Func<MainWindow, ThemedUserControl>? controlFactory = null,
    string[]? options = null,
    FoxyFileManager.ItemType? itemType = null,
    bool allowWebsiteUris = false)
    : Attribute
{
    public string? Name { get; } = name; // null = use field name
    public string? Description { get; } = description; // null = "" as description
    public SettingsCategory Category { get; } = category; // not null
    
    // extra for specific controls
    public string[]? Options { get; } = options;
    public FoxyFileManager.ItemType? ItemType { get; } = itemType;
    public bool AllowWebsiteUris { get; } = allowWebsiteUris;
    //TODO
    
    public Func<MainWindow, ThemedUserControl>? ControlFactory { get; } = controlFactory; // null = premade control
}

public enum SettingsCategory
{
    General,
}

public partial class BrowserSettings : ObservableObject
{
    public List<ISetting> GetSettingControls(MainWindow mainWindow)
    {
        var controls = new List<ISetting>(); //TODO: clump into one for now, but need a matrix here for categories.
        var fields = this.GetType().GetFields();
        foreach (var field in fields)
        {
            var attributes = (SettingInfoAttribute[])field.GetCustomAttributes(typeof(SettingInfoAttribute), false);
            
            if (attributes.FirstOrDefault() is not { } attribute) continue;
            
            if (attribute.ControlFactory is not null)
                controls.Add(new CustomControlSetting(attribute.Name ?? field.Name, attribute.Description ?? "", attribute.ControlFactory));
            else
                switch (field.GetValue(this))
                {
                    case bool b:
                        controls.Add(new BoolSetting(attribute.Name ?? field.Name, attribute.Description ?? "", b, b1 => field.SetValue(this, b1)));
                        break;
                    case int i:
                        controls.Add(new IntSetting(attribute.Name ?? field.Name, attribute.Description ?? "", i, i1 => field.SetValue(this, i1)));
                        break;
                    case decimal d:
                        controls.Add(new DecimalSetting(attribute.Name ?? field.Name, attribute.Description ?? "", d, d1 => field.SetValue(this, d1)));
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
                            controls.Add(new StringSetting(attribute.Name ?? field.Name, attribute.Description ?? "", s, s1 => field.SetValue(this, s1)));
                        break;
                    case Color c:
                        controls.Add(new ColorSetting(attribute.Name ?? field.Name, attribute.Description ?? "", c, c1 => field.SetValue(this, c1)));
                        break;
                    case Uri u:
                        throw new NotImplementedException();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Type '{field.FieldType.Name}' from field '{field.Name}' is not supported.'");
                }
        }
        
        return [];
    }

    #region General
    [SettingInfo(SettingsCategory.General)]
    public string TestString = "default value here";
    
    //TODO: enum support would be nice here:
    // dictionary map of enum to human readable.
    // detect enum, grab all values as options.
    [SettingInfo(SettingsCategory.General, options: ["0", "1", "2", "3"])]
    public string TestCombo = "1";
    
    [SettingInfo(SettingsCategory.General)]
    public bool TestBool = true;
    
    [SettingInfo(SettingsCategory.General, "Another Test Name")]
    public int TestInt = 42;
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
     #1#
    //TODO: need default static setting to use if none.
}

public partial class PerformanceSettings : ObservableObject
{
    
}*/