using System.Threading;
using FoxyBrowser716.DataManagement;

namespace FoxyBrowser716.Controls.HomePage.Widgets;

[WidgetInfo("Date/Time Widget", MaterialIconKind.Clock, WidgetCategory.TimeDate)]
public partial class DateTimeWidget : WidgetBase
{
	protected DateTimeWidget()
	{
		InitializeComponent();
	}

	private Timer refreshTimer;

    protected override async Task Initialize()
    {
	    refreshTimer = new Timer(RefreshTimer_Tick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
	    ApplyTheme();
    }

    protected override void ApplyTheme()
    {
        TimeOfTheBlock.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        DateOfTheBlock.Foreground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor);
        RootGrid.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        RootGrid.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);
    }

    private void RefreshTimer_Tick(object? state)
    {
	    AppServer.UiDispatcherQueue.TryEnqueue(() =>
	    {
		    try
		    {
			    TimeOfTheBlock.Text = DateTime.Now.ToLongTimeString();
			    DateOfTheBlock.Text = DateTime.Now.ToLongDateString();
		    }
		    catch
		    {
			    // stop jumping to here on application close!
			    // I don't care about an error when the application is already stopped.
		    }
	    });
    }
}
