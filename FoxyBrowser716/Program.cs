using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using WinRT;

namespace FoxyBrowser716;

/// <summary>
/// Custom entry point. Suppresses the XAML-generated Main so we can call
/// <see cref="Bootstrap.Initialize"/> before <see cref="Application.Start"/>
/// when running without an MSIX package (e.g. directly from the output folder or via Proton).
/// </summary>
internal static class Program
{
    // Native check — safe to call before any WinRT or WinAppSDK code is loaded.
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int GetCurrentPackageFullName(
        ref uint packageFullNameLength,
        [Optional] System.Text.StringBuilder? packageFullName);

    // ERROR_NO_PACKAGE_IDENTITY (APPMODEL_ERROR_NO_PACKAGE)
    private const int AppModelErrorNoPackage = 15700;

    [STAThread]
    internal static void Main(string[] args)
    {
        uint packageFullNameLength = 0;
        if (GetCurrentPackageFullName(ref packageFullNameLength) == AppModelErrorNoPackage)
        {
            // Load the Windows App SDK runtime DLLs from the system MSIX framework
            // package before Application.Start().  Without this call, any WinRT API that
            // requires package identity will throw immediately.
            // Version encoding: (major << 16) | minor — here major=1, minor=8 (WinAppSDK 1.8).
            const uint winAppSdkVersion18 = (1u << 16) | 8u;
            Bootstrap.Initialize(winAppSdkVersion18);
        }

        ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}
