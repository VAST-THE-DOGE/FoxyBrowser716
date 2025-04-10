namespace FoxyBrowser716.HomeWidgets.WidgetSettings;

public interface IWidgetSetting
{
	object Value { get; set; } // Using object to be super flexible~ *winks*
	event Action<object> ValueChanged; // Non-generic event
}

public interface IWidgetSetting<T> : IWidgetSetting
{
	new T Value { get; set; } // Override with the typed version
	new event Action<T> ValueChanged; // Typed event
}

public abstract class WidgetSetting<T> : IWidgetSetting<T>
{
	private T _value;

	public T Value
	{
		get => _value;
		set
		{
			_value = value;
			ValueChanged?.Invoke(value);
		}
	}

	object IWidgetSetting.Value
	{
		get => Value;
		set => Value = (T)value;
	}

	public event Action<T>? ValueChanged;

	event Action<object> IWidgetSetting.ValueChanged
	{
		add { ValueChanged += v => value?.Invoke(v); }
		remove { ValueChanged -= v => value?.Invoke(v); }
	}

	protected WidgetSetting(T value)
	{
		_value = value;
	}
}