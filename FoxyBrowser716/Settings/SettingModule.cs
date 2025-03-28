namespace FoxyBrowser716.Settings;

public class SettingModule<T>
{
	public string SettingName { get; init; }
	
	Func<T> Getter { get; }
	Action<T> Setter { get; }
	
	public event Action<T>? SettingUpdated;
	
	public T GetSetting() => Getter();
	public void SetSetting(T value)
	{
		Setter(value);
		SettingUpdated?.Invoke(value);
	}

	protected SettingModule(string settingName, Func<T> getter, Action<T> setter)
	{
		SettingName = settingName;
		Getter = getter;
		Setter = setter;
	}
}