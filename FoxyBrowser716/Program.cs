using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using WinRT;

namespace FoxyBrowser716;

internal static class Program
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int GetCurrentPackageFullName(
        ref uint packageFullNameLength,
        [Optional] System.Text.StringBuilder? packageFullName);

    private const int AppModelErrorNoPackage = 15700;

    [STAThread]
    internal static void Main(string[] args)
    {
        uint packageFullNameLength = 0;
        if (GetCurrentPackageFullName(ref packageFullNameLength) == AppModelErrorNoPackage)
        {
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
