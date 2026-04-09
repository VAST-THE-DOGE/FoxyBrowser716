namespace FoxyBrowser716.StaticData;

/// <summary>
/// Detects whether the app is running inside an MSIX package (packaged) or directly as an exe (unpackaged).
/// Call sites that use packaged-only Windows APIs must check <see cref="IsPackaged"/> first.
/// </summary>
public static class PackageHelper
{
    /// <summary>
    /// True when the process has MSIX package identity; false when running unpackaged (e.g. via Proton).
    /// </summary>
    public static readonly bool IsPackaged = CheckIsPackaged();

    private static bool CheckIsPackaged()
    {
        try
        {
            // Package.Current.Id throws InvalidOperationException when there is no package identity.
            _ = Windows.ApplicationModel.Package.Current.Id;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
