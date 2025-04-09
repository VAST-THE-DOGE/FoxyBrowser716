namespace FoxyBrowser716.HomeWidgets.WidgetSettings;

public class WidgetSettingBool(string name, bool value) : WidgetSetting<bool>(name, value);
public class WidgetSettingInt(string name, int value) : WidgetSetting<int>(name, value);
public class WidgetSettingDouble(string name, double value) : WidgetSetting<double>(name, value);
public class WidgetSettingString(string name, string value) : WidgetSetting<string>(name, value);