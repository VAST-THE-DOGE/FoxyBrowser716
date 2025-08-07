namespace FoxyBrowser716_WinUI.DataObjects.Settings;

public static class SettingsHelper
{
	public static UIElement GetEditor(this ISetting setting)
	{
		switch (setting)
		{
			case DividerSetting dividerSetting:
				return new UserControl(); //TODO
			case HeaderSetting headerSetting:
				return new UserControl(); //TODO
			case SubheadingSetting subheadingSetting:
				return new UserControl(); //TODO

			
			case BoolSetting boolSetting:
				return new UserControl(); //TODO
			case IntSetting intSetting:
				return new UserControl(); //TODO
			case DecimalSetting intSetting:
				return new UserControl(); //TODO
			case StringSetting stringSetting:
				return new UserControl(); //TODO
			
			case ColorSetting colorSetting:
				return new UserControl(); //TODO
			case FilePickerSetting filePickerSetting:
				return new UserControl(); //TODO
			case CustomControlSetting customControlSetting:
				return customControlSetting.ControlFactory(); //TODO
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}