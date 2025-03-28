using System.Windows;
using System.Windows.Input;

namespace FoxyBrowser716;

public partial class TabMoveWindowCard : Window
{
    private WebsiteTab Tab;
    private ServerManager Instance;

    public TabMoveWindowCard(ServerManager instance, WebsiteTab tab)
    {
        InitializeComponent();
        Instance = instance;
        Tab = tab;
        
        TitleLabel.Content = Tab.Title;
        TabIcon.Child = Tab.Icon;

        Loaded += (_,_) =>
        {
            DragMove();
        };

        var dragStarted = false;
        MouseMove += (_, _) =>
        {
            if (!dragStarted && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
                dragStarted = true;
            }
        };
        
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    private async void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var finalPos = new Point(Left, Top);
        await Instance.CreateWindowFromTab(Tab, finalPos);
        Close();
    }
}
