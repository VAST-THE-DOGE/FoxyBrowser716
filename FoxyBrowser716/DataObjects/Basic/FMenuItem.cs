namespace FoxyBrowser716.DataObjects.Basic;

public class FMenuItem
{
	public string Text { get; set; } = string.Empty;
	public Action? Action { get; set; }

	public void ClickedEvent(object sender, RoutedEventArgs e)
	{
		Action?.Invoke();
	}
}