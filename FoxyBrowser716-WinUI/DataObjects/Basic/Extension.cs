using FoxyBrowser716_WinUI.DataManagement;

namespace FoxyBrowser716_WinUI.DataObjects.Basic;

public class Extension
{
	public string FolderPath { get; init; }
	public bool IsEnabled { get; init; } //TODO
	public string Id { get; init; }
	public ExtensionManifestBase Manifest { get; init; }
}