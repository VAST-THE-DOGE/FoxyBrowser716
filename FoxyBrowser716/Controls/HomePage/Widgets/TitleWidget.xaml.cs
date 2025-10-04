using System.Threading;
using FoxyBrowser716.DataManagement;

namespace FoxyBrowser716.Controls.HomePage.Widgets;

[WidgetInfo("Title Widget", MaterialIconKind.FormatTitle, WidgetCategory.Misc)]
public partial class TitleWidget : WidgetBase
{
	protected TitleWidget()
	{
		InitializeComponent();
	}
	
    protected override async Task Initialize()
    {
	    ApplyTheme();
    }

    protected override void ApplyTheme()
    {
        TitleOfTheBlock.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        RootGrid.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        RootGrid.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);
        
        ButtonGithub.CurrentTheme = CurrentTheme;
        ButtonWebsite.CurrentTheme = CurrentTheme;
    }

    private void ButtonGithub_OnClick(object sender, RoutedEventArgs e)
    {
	    TabManager.SwapActiveTabTo(TabManager.AddTab(InfoGetter.GitHubUrl));
    }

    private void ButtonWebsite_OnClick(object sender, RoutedEventArgs e)
    {
	    TabManager.SwapActiveTabTo(TabManager.AddTab(InfoGetter.WebsiteUrl));
    }
}
