using System.Windows;

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
        
    }

    private void DragEnd()
    {
        //TODO
    }

    private void MouseMoved()
    {
        //TODO
    }
}