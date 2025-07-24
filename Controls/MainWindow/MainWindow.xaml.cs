using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using WinRT.Interop;

//

using Microsoft.UI.Xaml.Input;

using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

// using CommunityToolkit.WinUI.Helpers;
//

namespace FoxyBrowser716_WinUI.Controls.MainWindow;

public sealed partial class MainWindow : WinUIEx.WindowEx
{ 
    public MainWindow()
    {
        InitializeComponent();
        Activated += (_, _) =>
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TopBar.DragZone); // initial is needed to allow clicks for other buttons
            var presenter = AppWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(true, false);
            }
        };
        
        TopBar.DragZone.PointerEntered += (_, _) =>
        {
            SetTitleBar(TopBar.DragZone); // to fix a bug with this becoming unset for whatever reason
        };
    }

    private void TopBar_OnMinimizeClicked()
    {
        throw new NotImplementedException();
    }

    private void TopBar_OnMaximizeClicked()
    {
        throw new NotImplementedException();
    }

    private void TopBar_OnCloseClicked()
    {
        throw new NotImplementedException();
    }
}