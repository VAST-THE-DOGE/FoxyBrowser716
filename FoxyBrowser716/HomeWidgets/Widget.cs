using System.Windows.Controls;

namespace FoxyBrowser716.HomeWidgets;

public abstract class IWidget : UserControl
{
	public abstract string WidgetName { get; }
	public abstract int MinWidgetWidth { get; }
	public abstract int MinWidgetHeight { get; }
	public abstract int MaxWidgetWidth { get; }
	public abstract int MaxWidgetHeight { get; }

	/// <summary>
	/// Initializes the widget with necessary setup operations or loading procedures.
	/// </summary>
	public abstract Task Initialize();
}