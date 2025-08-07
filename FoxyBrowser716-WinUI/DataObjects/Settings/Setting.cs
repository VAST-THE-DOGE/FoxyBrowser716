using System.ComponentModel;

namespace FoxyBrowser716_WinUI.DataObjects.Settings;

[ObservableObject]
public abstract partial class Setting<T> : ISetting
{
	public string Name { get; }
	public string Description { get; }

	[ObservableProperty] public partial T Value { get; set; }
        
	protected Setting(string name, string description, T defaultValue)
	{
		Name = name;
		Description = description;
		Value = defaultValue;
	}
}