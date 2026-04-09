using System.Runtime.InteropServices;

namespace FoxyBrowser716.StaticData;

/// <summary>
/// Detects whether the app is running inside an MSIX package (packaged) or directly as an exe (unpackaged).
/// Uses a native Win32 call so it is safe to invoke before any WinRT or Windows App SDK code is loaded.
/// Call sites that use packaged-only Windows APIs must check <see cref="IsPackaged"/> first.
/// </summary>
public static class PackageHelper
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int GetCurrentPackageFullName(
        ref uint packageFullNameLength,
        [Optional] System.Text.StringBuilder? packageFullName);

    // ERROR_NO_PACKAGE_IDENTITY (APPMODEL_ERROR_NO_PACKAGE)
    private const int AppModelErrorNoPackage = 15700;

    /// <summary>
    /// True when the process has MSIX package identity; false when running unpackaged (e.g. via Proton).
    /// </summary>
    public static readonly bool IsPackaged = CheckIsPackaged();

    private static bool CheckIsPackaged()
    {
        uint packageFullNameLength = 0;
        return GetCurrentPackageFullName(ref packageFullNameLength) != AppModelErrorNoPackage;
    }
}
