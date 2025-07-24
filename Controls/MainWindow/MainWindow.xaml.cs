using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics.Display;
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
        
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TopBar.DragZone); // initial is needed to allow clicks for other buttons
        var presenter = AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(true, false);
        }
        
        TopBar.DragZone.PointerEntered += (_, _) =>
        {
            SetTitleBar(TopBar.DragZone); // to fix a bug with this becoming unset for whatever reason
        };

        this.WindowStateChanged += HandleWindowStateChanged;
    }

    private void TopBar_OnMinimizeClicked()
    {
        if (InFullscreen)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.Default);
        }

        this.Minimize();
    }

    private void TopBar_OnMaximizeClicked()
    {
        if (!InFullscreen && WindowState == WindowState.Normal)
        {
            if (TopBar.IsBorderless)
                AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            else
                this.Maximize();
        }
        else if (InFullscreen)
            AppWindow.SetPresenter(AppWindowPresenterKind.Default);
        else if (WindowState == WindowState.Maximized)
            this.Restore();
    }

    private void TopBar_OnCloseClicked()
    {
        this.Close();
    }
    
    private void TopBar_OnBorderlessToggled()
    {
        if (!TopBar.IsBorderless && InFullscreen)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.Default);
            if (WindowState != WindowState.Maximized)
            {
                this.Maximize();
            }
        }
        else if (TopBar.IsBorderless && WindowState == WindowState.Maximized)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
    }
    
    internal bool InFullscreen => AppWindow.Presenter is FullScreenPresenter;
    
    private void HandleWindowStateChanged(object? s, WindowState ws)
    {
        //TODO: very weird here!
        // need the following to make maximize by drag or windows arrow keys to work properly with fullscreen
        
        // if (ws == WindowState.Maximized && !InFullscreen && TopBar.IsBorderless)
        //     AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }
}