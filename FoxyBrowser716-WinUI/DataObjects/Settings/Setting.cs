using System.ComponentModel;

namespace FoxyBrowser716_WinUI.DataObjects.Settings;

[ObservableObject]
public abstract partial class Setting<T> : ISetting
{
	public string Name { get; }
	public string Description { get; }

	[ObservableProperty] public partial T Value { get; set; } 
	
	//TODO: need an on change action in constructor. Will make code a lot cleaner
	protected Setting(string name, string description, T defaultValue, Action<T>? onValueChanged = null)
	{
		Name = name;
		Description = description;
		Value = defaultValue;

		PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(Value))
				onValueChanged?.Invoke(Value);
		};
	}
}