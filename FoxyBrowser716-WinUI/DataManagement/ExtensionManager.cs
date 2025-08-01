using FoxyBrowser716_WinUI.DataObjects.Basic;

namespace FoxyBrowser716_WinUI.DataManagement;

public static class ExtensionManager
{
	public static void SetupExtensionSupport(this WebView2 webview)
	{
		//TODO:
	}
	
	public static async Task<List<Extension>> GetExtensions(this Instance instance)
	{
		var extensionFolder = FoxyFileManager.BuildFolderPath(FoxyFileManager.FolderType.Extension, instance.Name);

		var folders = await FoxyFileManager.GetChildrenOfFolderAsync(extensionFolder, FoxyFileManager.ItemType.Folder);

		if (folders.items is null) return [];
		
		List<Extension> extensions = [];
		
		foreach (var item in folders.items)
		{
			var manifestFile = Directory.GetFiles(item.path, "manifest.json", SearchOption.TopDirectoryOnly)
				.FirstOrDefault();
			
			//TODO: need more for creating an extension object, but keep it simple for now.
			
			if (manifestFile is { })
				extensions.Add(new Extension { FolderPath = item.path});
		}

		return extensions;
	}
}