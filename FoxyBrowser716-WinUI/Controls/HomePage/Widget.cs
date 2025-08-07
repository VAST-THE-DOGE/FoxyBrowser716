using System.Reflection;
using System.Runtime.CompilerServices;
using FoxyBrowser716_WinUI.DataManagement;
using FoxyBrowser716_WinUI.DataObjects.Settings;
using Material.Icons;

namespace FoxyBrowser716_WinUI.Controls.HomePage;

public abstract class WidgetBase : UserControl
{
    internal WidgetData LayoutData { get; set; } = null!;
    
    public static string GetWidgetName<T>() where T : WidgetBase => 
        typeof(T).GetCustomAttribute<WidgetInfoAttribute>()?.Name
            ?? throw new InvalidOperationException("Widget does not have a WidgetInfoAttribute");
    
    public static MaterialIconKind GetWidgetIcon<T>() where T : WidgetBase => 
        typeof(T).GetCustomAttribute<WidgetInfoAttribute>()?.Icon 
            ?? throw new InvalidOperationException("Widget does not have a WidgetInfoAttribute");
    
    [ModuleInitializer]
    internal static void InitializeWidgets()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var widgetTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(WidgetBase)) && !t.IsAbstract);
        
        foreach (var type in widgetTypes)
        {
            RegisterWidgetType(type);
        }
    }
    
    private static void RegisterWidgetType(Type type)
    {
        var attr = type.GetCustomAttribute<WidgetInfoAttribute>();
        
        if (attr != null)
        {
            HomePage.AddWidget(
                attr.Name,
                attr.Icon,
                attr.Category,
                async (manager, settings, layoutData) => 
                {
                    var widget = (WidgetBase)Activator.CreateInstance(type, nonPublic: true);
                    await widget.InitializeBase(manager, settings);
                    await widget.Initialize();
                    widget.LayoutData = layoutData;
                    return widget;

                }
            );
        }
        else
            throw new InvalidOperationException($"Widget {type.Name} must have a WidgetInfoAttribute");
    }
    
    public TabManager TabManager = null!;

    public Theme CurrentTheme { get; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

    protected abstract void ApplyTheme();
    
    public List<ISetting> WidgetSettings = [];
    
    private protected WidgetBase() {}

    protected async Task InitializeBase(TabManager manager, Dictionary<string, object>? settingsMap = null)
    {
        TabManager = manager;

        foreach (var rawSetting in WidgetSettings)
            SetSetting(rawSetting, settingsMap);
    }
    
    protected abstract Task Initialize();
    
    public Dictionary<string, object> GetSettingsMap()
    {
        var settingsMap = new Dictionary<string, object>();
        
        foreach (var setting in WidgetSettings)
        {
            var settingType = setting.GetType();
        
            if (settingType.IsGenericType && settingType.GetGenericTypeDefinition() == typeof(Setting<>))
            {
                var nameProperty = settingType.GetProperty("Name");
                var valueProperty = settingType.GetProperty("Value");
            
                if (nameProperty != null && valueProperty != null)
                {
                    var settingName = nameProperty.GetValue(setting) as string;
                    var settingValue = valueProperty.GetValue(setting);
                
                    if (settingName != null && settingValue != null)
                    {
                        settingsMap[settingName] = settingValue;
                    }
                }
            }
        }
    
        return settingsMap;
    }
    
    private static void SetSetting(ISetting rawSetting, Dictionary<string,object>? settingsMap)
    {
        if (settingsMap is null) return;
        var settingType = rawSetting.GetType();
        if (!settingType.IsGenericType || settingType.GetGenericTypeDefinition() != typeof(Setting<>)) return;
        var valueType = settingType.GetGenericArguments()[0];
        var nameProperty = settingType.GetProperty("Name");
        var valueProperty = settingType.GetProperty("Value");
        if (nameProperty is null || valueProperty is null) return;
        if (nameProperty.GetValue(rawSetting) is not string settingName || !settingsMap.TryGetValue(settingName, out var rawValue)) return;
        if (rawValue is not null && valueType.IsInstanceOfType(rawValue))
        {
            valueProperty.SetValue(rawSetting, rawValue);
        }
        else if (rawValue is null && !valueType.IsValueType)
        {
            valueProperty.SetValue(rawSetting, null);
        }
        else if (rawValue is not null)
        {
            try
            {
                var convertedValue = Convert.ChangeType(rawValue, valueType);
                valueProperty.SetValue(rawSetting, convertedValue);
            }
            catch { /*ignored*/ }
        }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class WidgetInfoAttribute : Attribute
{
    public string Name { get; }
    public MaterialIconKind Icon { get; }
    public WidgetCategory Category { get; }

    public WidgetInfoAttribute(string name, MaterialIconKind icon, WidgetCategory category)
    {
        Name = name;
        Icon = icon;
        Category = category;
    }
}

public enum WidgetCategory
{
    General,
    TimeDate,
    WebsiteNavigation,
    FoxyBrowser716,
    Misc,
}

public static class WidgetCategoryExtensions
{
    public static MaterialIconKind GetIcon(this WidgetCategory category)
    {
        return category switch
        {
            WidgetCategory.General => MaterialIconKind.Toolbox,
            WidgetCategory.TimeDate => MaterialIconKind.Clock,
            WidgetCategory.WebsiteNavigation => MaterialIconKind.Web,
            WidgetCategory.FoxyBrowser716 => MaterialIconKind.ApplicationCog,
            WidgetCategory.Misc => MaterialIconKind.Inbox,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }
    
    public static string GetName(this WidgetCategory category)
    {
        return category switch
        {
            WidgetCategory.General => "General",
            WidgetCategory.TimeDate => "Time & Date",
            WidgetCategory.WebsiteNavigation => "Website Navigation",
            WidgetCategory.FoxyBrowser716 => "FoxyBrowser716",
            WidgetCategory.Misc => "Miscellaneous",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }
}