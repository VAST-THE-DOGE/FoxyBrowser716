using System.Runtime.InteropServices;
using WinRT.Interop;

namespace FoxyBrowser716.Controls.Generic;

public class TransparentWindow : Window
{
	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	private const int GWL_EXSTYLE = -20;
	private const int WS_EX_LAYERED = 0x80000;

	public TransparentWindow()
	{
		var hWnd = WindowNative.GetWindowHandle(this);

		// Set layered window style
		var extendedStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
		SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED);
	}
}