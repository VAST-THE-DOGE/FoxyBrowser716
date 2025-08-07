namespace FoxyBrowser716_WinUI.DataObjects.Settings;

// formating
public class DividerSetting() : ISetting { public string Name => "Divider"; }

public class HeaderSetting(string name) : ISetting { public string Name => name; }
public class SubheadingSetting(string name) : ISetting { public string Name => name; }

// value types
public class BoolSetting(string name, string description, bool defaultValue) : Setting<bool>(name, description, defaultValue);
public class IntSetting(string name, string description, int defaultValue) : Setting<int>(name, description, defaultValue);
public class DecimalSetting(string name, string description, decimal defaultValue) : Setting<decimal>(name, description, defaultValue);
public class StringSetting(string name, string description, string defaultValue) : Setting<string>(name, description, defaultValue);

// complex types
public class ColorSetting(string name, string description, Color defaultValue) : Setting<Color>(name, description, defaultValue);
public class FilePickerSetting(string name, string description, string defaultValue) : Setting<string>(name, description, defaultValue);
public class FolderPickerSetting(string name, string description, string defaultValue) : Setting<string>(name, description, defaultValue);
public class CustomControlSetting(string name, string description, Func<UIElement> controlFactory) : ISetting { public string Name => name; public string Description => description; public Func<UIElement> ControlFactory => controlFactory; }
