using System.Windows.Controls;
using FoxyBrowser716.HomeWidgets.WidgetSettings;

namespace FoxyBrowser716.HomeWidgets;

public abstract class IWidget : UserControl
{
	public abstract string WidgetName { get; }
	public virtual int MinWidgetWidth => 1;
	public virtual int MinWidgetHeight => 1;
	public virtual int MaxWidgetWidth => 40;
	public virtual int MaxWidgetHeight => 20;

	public virtual bool CanRemove => true;
	public virtual bool CanAdd => true;
	public virtual bool CanEdit => true;

	private bool _inWidgetEditingMode;
	public bool InWidgetEditingMode { get => _inWidgetEditingMode; set => SetEditMode(value); }

	public Dictionary<string, IWidgetSetting>? WidgetSettings;


	/// <summary>
	/// Initializes the widget with necessary setup operations or loading procedures.
	/// </summary>
	public virtual Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings)
	{
		WidgetSettings = settings;
		
		return Task.CompletedTask;
	}

	/// <summary>
	/// Can be overridden to have custom logic when entering/exiting edit mode
	/// </summary>
	private protected virtual void SetEditMode(bool value)
	{
		_inWidgetEditingMode = value;
	}
}