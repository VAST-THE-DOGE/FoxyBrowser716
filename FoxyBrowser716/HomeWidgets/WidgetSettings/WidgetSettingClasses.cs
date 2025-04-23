using System.Windows.Media;

namespace FoxyBrowser716.HomeWidgets.WidgetSettings;

// values
public class WidgetSettingBool(bool value) : WidgetSetting<bool>(value);
public class WidgetSettingInt(int value) : WidgetSetting<int>(value);
public class WidgetSettingDouble(double value) : WidgetSetting<double>(value);
public class WidgetSettingString(string value) : WidgetSetting<string>(value);
public class WidgetSettingFolderPicker(string value) : WidgetSetting<string>(value);
public class WidgetSettingFilePicker(string value) : WidgetSetting<string>(value);


public class WidgetSettingCombo(string value, string[] options) : WidgetSetting<string>(value)
{
	public readonly string[] Options = options;
};
public class WidgetSettingColor(Color value) : WidgetSetting<Color>(value);

// formating
public class WidgetSettingSubTitle(string value) : WidgetSetting<string>(value);
public class WidgetSettingDescription(string value) : WidgetSetting<string>(value);
public class WidgetSettingDiv() : WidgetSetting<object?>(null);