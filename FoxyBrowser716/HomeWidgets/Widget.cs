using System.Windows.Controls;

namespace FoxyBrowser716.HomeWidgets;

public abstract class IWidget : UserControl
{
	public abstract string WidgetName { get; }
	public virtual int MinWidgetWidth => 1;
	public virtual int MinWidgetHeight => 1;
	public virtual int MaxWidgetWidth => 40;
	public virtual int MaxWidgetHeight => 20;

	/// <summary>
	/// Initializes the widget with necessary setup operations or loading procedures.
	/// </summary>
	public abstract Task Initialize();
}