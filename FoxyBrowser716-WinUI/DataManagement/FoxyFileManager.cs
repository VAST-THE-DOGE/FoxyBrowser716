namespace FoxyBrowser716_WinUI.DataManagement;

/// <summary>
/// Although working with the file system is really easy in C#,
/// this is used to make life easier and to keep things consistent.
/// </summary>
public static class FoxyFileManager
{
	#region FolderNames
	/// <summary>
	/// just the app name, from the info getter.
	/// </summary>
	private static readonly string BrowserDataFolderName = InfoGetter.AppName;
	/// <summary>
	/// the name of the folder that contains all instances of the browser.
	/// </summary>
	private const string InstanceFolderName = "Instances";
	/// <summary>
	/// the name of the folder that contains the extensions for an instance.
	/// </summary>
	private const string ExtensionFolderName = "Extensions";
	/// <summary>
	/// the name of the folder that contains webview2 data for an instance.
	/// </summary>
	private const string WebView2FolderName = "WebView2";
	/// <summary>
	/// the name of the folder that contains all permanent data of the browser (data that shouldn't be deleted).
	/// </summary>
	private const string DataFolderName = "Data";
	/// <summary>
	/// the name of the folder that contains all temporary data of the browser (data that can be deleted with little to no impact).
	/// </summary>
	private const string CacheFolderName = "Cache";
	/// <summary>
	/// the name of the folder that contains all widget data for an instance.
	/// </summary>
	private const string WidgetsFolderName = "Widgets";
	#endregion

	#region FullPaths
	/// <summary>
	/// Just appdata and the app name.
	/// </summary>
	private static readonly string BrowserAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BrowserDataFolderName);
	/// <summary>
	/// folder that contains all instances of the browser.
	/// </summary>
	private static readonly string InstanceFolderPath = Path.Combine(BrowserAppDataPath, InstanceFolderName);
	/// <summary>
	/// all permanent data of the browser (data that shouldn't be deleted).
	/// </summary>
	private static readonly string BrowserDataFolderPath = Path.Combine(BrowserAppDataPath, DataFolderName);
	/// <summary>
	/// all temporary data of the browser (data that can be deleted with little to no impact).
	/// </summary>
	private static readonly string CacheFolderPath = Path.Combine(BrowserAppDataPath, CacheFolderName);
	#endregion

	#region Enums
	/// <summary>
	/// represents a custom return code instead of throwing exceptions everywhere.
	/// </summary>
	public enum ReturnCode
	{
		Success,
		NotFound,
		AlreadyExists,
		Unauthorized,
		InvalidPath,
		UnknownError,
	}
	
	/// <summary>
	/// represents the type of folder to build a path for.
	/// </summary>
	public enum FolderType
	{
		BrowserData,
		Instance,
		Extension,
		WebView2,
		Data,
		Cache,
		Widgets,
	}
	
	/// <summary>
	/// provides a way to distinguish between files and folders.
	/// </summary>
	public enum ItemType
	{
		File,
		Folder
	}
	#endregion

	#region PathBuilders
	/// <summary>
	/// Builds a path to a specific folder type.
	/// </summary>
	/// <param name="folderType">the folder</param>
	/// <param name="instanceName">name of the instance to build the path for if it is applicable
	/// (will result in different paths)</param>
	/// <returns>a folder path as a string</returns>
	/// <exception cref="ArgumentNullException">The instance path is needed for the following folders:
	/// Extension, WebView2, and Widgets.
	/// Not including the instance name when using these folders will result in this exception.</exception>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static string BuildFolderPath(FolderType folderType, string? instanceName = null)
	{
		return folderType switch
		{
			FolderType.BrowserData => BrowserAppDataPath,
			FolderType.Instance => instanceName is null ? InstanceFolderPath : Path.Combine(InstanceFolderPath, instanceName),
			FolderType.Extension => instanceName is null ? throw new ArgumentNullException(nameof(instanceName)) : Path.Combine(InstanceFolderPath, instanceName, ExtensionFolderName),
			FolderType.WebView2 => instanceName is null ? throw new ArgumentNullException(nameof(instanceName)) : Path.Combine(InstanceFolderPath, instanceName, WebView2FolderName),
			FolderType.Widgets => instanceName is null ? throw new ArgumentNullException(nameof(instanceName)) : Path.Combine(InstanceFolderPath, instanceName, DataFolderName, WidgetsFolderName),
			FolderType.Data => instanceName is null ? BrowserDataFolderPath : Path.Combine(InstanceFolderPath, instanceName, DataFolderName),
			FolderType.Cache => instanceName is null ? CacheFolderPath : Path.Combine(InstanceFolderPath, instanceName, CacheFolderName),
			_ => throw new ArgumentOutOfRangeException(nameof(folderType), folderType, null)
		};
	}
	
	/// <summary>
	/// Builds a path to access a file within a specific folder type.
	/// </summary>
	/// <param name="fileName">file name including the extension to build the path for</param>
	/// <param name="folderType">type of the folder to build a path to.</param>
	/// <param name="instanceName">name of the instance to build the path for if it is applicable
	/// (will result in different paths)</param>
	/// <returns>a file path as a string</returns>
	/// <exception cref="ArgumentNullException">The instance path is needed for the following folders:
	/// Extension, WebView2, and Widgets.
	/// Not including the instance name when using these folders will result in this exception.</exception>
	/// <exception cref="ArgumentException">The file name cannot be empty.</exception>
	public static string BuildFilePath(string fileName, FolderType folderType, string? instanceName = null)
	{
		if (string.IsNullOrWhiteSpace(fileName))
			throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
		
		return Path.Combine(BuildFolderPath(folderType, instanceName), fileName);
	}
	#endregion

	#region FolderManagement

		#region CreateFolder
		/// <summary>
		/// Creates a folder at the specified path.
		/// It is recommended to use the BuildFolderPath method to get the path.
		/// </summary>
		/// <param name="folderPath">path of the folder to create</param>
		/// <returns>ReturnCode.[InvalidPath,AlreadyExists,Success,Unauthorized,UnknownError]</returns>
		public static ReturnCode CreateFolder(string folderPath)
		{
			try
			{
				// safety check to ensure the folder path is valid and within the app's data directory
				if (string.IsNullOrWhiteSpace(folderPath) || !folderPath.StartsWith(BrowserAppDataPath))
					return ReturnCode.InvalidPath;
			
				if (Directory.Exists(folderPath))
					return ReturnCode.AlreadyExists;

				Directory.CreateDirectory(folderPath);
				return ReturnCode.Success;
			}
			catch (UnauthorizedAccessException)
			{
				return ReturnCode.Unauthorized;
			}
			catch (Exception)
			{
				//TODO: log this error
				return ReturnCode.UnknownError;
			}
		}
	
		/// <summary>
		/// Creates a folder at the specified path.
		/// It is recommended to use the BuildFolderPath method to get the path.
		/// Note that this method is asynchronous, but Directory.CreateDirectory is not, so it wraps the synchronous method in a Task.
		/// </summary>
		/// <param name="folderPath">path of the folder to create</param>
		/// <returns>A task containing: ReturnCode.[InvalidPath,AlreadyExists,Success,Unauthorized,UnknownError]</returns>
		public static async Task<ReturnCode> CreateFolderAsync(string folderPath)
		{
			// no async method for Directory.CreateDirectory, so wrap it in a Task
			return await Task.Run(() => CreateFolder(folderPath));
		}
		#endregion

		#region DeleteFolder
		/// <summary>
		/// Deletes a folder at the specified path.
		/// It is recommended to use the BuildFolderPath method to get the path.
		/// Note that only folders within the instance folder can be deleted.
		/// </summary>
		/// <param name="folderPath">the path to the folder to delete</param>
		/// <returns>ReturnCode.[InvalidPath,NotFound,Success,Unauthorized,UnknownError]</returns>
		public static ReturnCode DeleteFolder(string folderPath)
		{
			try
			{
				// safety check to ensure the folder path is valid and within the app's instance directory
				if (string.IsNullOrWhiteSpace(folderPath) || !folderPath.StartsWith(InstanceFolderPath))
					return ReturnCode.InvalidPath;

				if (!Directory.Exists(folderPath))
					return ReturnCode.NotFound;

				Directory.Delete(folderPath, true);
				return ReturnCode.Success;
			}
			catch (UnauthorizedAccessException)
			{
				return ReturnCode.Unauthorized;
			}
			catch (Exception)
			{
				//TODO: log this error
				return ReturnCode.UnknownError;
			}
		}

		/// <summary>
		/// Deletes a folder at the specified path.
		/// It is recommended to use the BuildFolderPath method to get the path.
		/// Note that only folders within the instance folder can be deleted.
		/// Note that this method is asynchronous, but Directory.Delete is not, so it wraps the synchronous method in a Task.
		/// </summary>
		/// <param name="folderPath">the path to the folder to delete</param>
		/// <returns>ReturnCode.[InvalidPath,NotFound,Success,Unauthorized,UnknownError]</returns>
		public static async Task<ReturnCode> DeleteFolderAsync(string folderPath)
		{
			// no async method for Directory.Delete, so wrap it in a Task
			return await Task.Run(() => DeleteFolder(folderPath));
		}
		#endregion

		#region GetAllItemsInFolder
		/// <summary>
		/// Returns topmost children (files and/or subfolders) of a folder.
		/// It is recommended to use the BuildFolderPath method to get the path.
		/// </summary>
		/// <param name="folderPath">path to the folder to search</param>
		/// <param name="itemTypeFilter">A filter that can be null to get everything, set to only files, or set to only folders</param>
		/// <returns>a tuple containing a return code and a nullable list of tuples containing the item type and the path to the item</returns>
		public static (ReturnCode code, List<(ItemType type, string path)>? items) GetChildrenOfFolder(
			string folderPath, ItemType? itemTypeFilter = null)
		{
			try
			{
				// safety check to ensure the folder path is valid and within the app's data directory
				if (string.IsNullOrWhiteSpace(folderPath) || !folderPath.StartsWith(BrowserAppDataPath))
					return (ReturnCode.InvalidPath, null);

				if (!Directory.Exists(folderPath))
					return (ReturnCode.NotFound, null);

				var items = new List<(ItemType type, string path)>();

				if (itemTypeFilter is ItemType.Folder or null)
					items.AddRange(Directory.GetDirectories(folderPath).Select(dir => (ItemType.Folder, dir)));
				
				if (itemTypeFilter is ItemType.File or null)
					items.AddRange(Directory.GetFiles(folderPath).Select(file => (ItemType.File, file)));

				return (ReturnCode.Success, items);
			}
			catch (UnauthorizedAccessException)
			{
				return (ReturnCode.Unauthorized, null);
			}
			catch (Exception)
			{
				return (ReturnCode.UnknownError, null);
			}
		}
		
		/// <summary>
		/// Returns topmost children (files and/or subfolders) of a folder.
		/// It is recommended to use the BuildFolderPath method to get the path.
		/// Note that this method is asynchronous, but Directory.GetDirectories and Directory.GetFiles are not, so it wraps the synchronous method in a Task.
		/// </summary>
		/// <param name="folderPath">path to the folder to search</param>
		/// <param name="itemTypeFilter">A filter that can be null to get everything, set to only files, or set to only folders</param>
		/// <returns>a task containing a tuple containing a return code and a nullable list of tuples containing the item type and the path to the item</returns>
		public static async Task<(ReturnCode code, List<(ItemType type, string path)>? items)> GetChildrenOfFolderAsync(
			string folderPath, ItemType? itemTypeFilter = null)
		{
			// no async method for Directory.GetDirectories or Directory.GetFiles, so wrap it in a Task
			return await Task.Run(() => GetChildrenOfFolder(folderPath, itemTypeFilter));
		}
		#endregion
		
	#endregion

	#region FileManagment

		#region ReadFromFile
		/// <summary>
		/// Reads all content from a file as plain text.
		/// It is recommended to use the BuildFilePath method to get the path.
		/// It is also recommended to use the ReadFileAsync method instead of this one, as it is asynchronous.
		/// </summary>
		/// <param name="filePath">the path to the file to read</param>
		/// <returns>a tuple containing the returnCode and a nullable string</returns>
		public static (ReturnCode code, string? content) ReadFromFile(string filePath)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath) || !filePath.StartsWith(BrowserAppDataPath))
					return (ReturnCode.InvalidPath, null);

				if (!File.Exists(filePath))
					return (ReturnCode.NotFound, null);

				return (ReturnCode.Success, File.ReadAllText(filePath));
			}
			catch (UnauthorizedAccessException)
			{
				return (ReturnCode.Unauthorized, null);
			}
			catch (Exception)
			{
				//TODO: log this error
				return (ReturnCode.UnknownError, null);
			}
		}

		/// <summary>
		/// Reads all content from a file as plain text.
		/// It is recommended to use the BuildFilePath method to get the path.
		/// </summary>
		/// <param name="filePath">the path to the file to read</param>
		/// <returns>a task containing a tuple containing the returnCode and a nullable string</returns>
		public static async Task<(ReturnCode code, string? content)> ReadFromFileAsync(string filePath)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath) || !filePath.StartsWith(BrowserAppDataPath))
					return (ReturnCode.InvalidPath, null);

				if (!File.Exists(filePath))
					return (ReturnCode.NotFound, null);

				return (ReturnCode.Success, await File.ReadAllTextAsync(filePath));
			}
			catch (UnauthorizedAccessException)
			{
				return (ReturnCode.Unauthorized, null);
			}
			catch (Exception)
			{
				//TODO: log this error
				return (ReturnCode.UnknownError, null);
			}
		}

		/// <summary>
		///Reads all text from a file and tries to deserialize it into the specified type.
		/// It is recommended to use the BuildFilePath method to get the path.
		/// It is also recommended to use the ReadFileAsync method instead of this one, as it is asynchronous.
		/// </summary>
		/// <param name="filePath">the path to the file</param>
		/// <typeparam name="T">the type to deserialize to</typeparam>
		/// <returns>a tuple containing a return code and null or the object that was read</returns>
		public static (ReturnCode code, T? content) ReadFromFile<T>(string filePath) where T : class
		{
			if (!filePath.EndsWith(".json"))
				return (ReturnCode.InvalidPath, null);
			
			var (code, content) = ReadFromFile(filePath);
			if (code != ReturnCode.Success || content is null)
				return (code, null);

			try
			{
				return (ReturnCode.Success, JsonSerializer.Deserialize<T>(content));
			}
			catch (Exception)
			{
				return (ReturnCode.UnknownError, null);
			}
		}

		/// <summary>
		///Reads all text from a file and tries to deserialize it into the specified type.
		/// It is recommended to use the BuildFilePath method to get the path.
		/// </summary>
		/// <param name="filePath">the path to the file</param>
		/// <typeparam name="T">the type to deserialize to</typeparam>
		/// <returns>a task containing a tuple containing a return code and null or the object that was read</returns>
		public static async Task<(ReturnCode code, T? content)> ReadFromFileAsync<T>(string filePath) where T : class
		{
			if (!filePath.EndsWith(".json"))
				return (ReturnCode.InvalidPath, null);
			
			var (code, content) = await ReadFromFileAsync(filePath);
			if (code != ReturnCode.Success || content is null)
				return (code, null);

			try
			{
				return (ReturnCode.Success, JsonSerializer.Deserialize<T>(content));
			}
			catch (Exception)
			{
				return (ReturnCode.UnknownError, null);
			}
		}
		#endregion

		#region SaveToFile
		/// <summary>
		/// Saves plain text content to a file at the specified path.
		/// Note that it is recommended to use the BuildFilePath method to get the path.
		/// Also note that this method is synchronous, but there is an asynchronous version available.
		/// </summary>
		/// <param name="filePath">path to the location to save to.</param>
		/// <param name="content">plain text content to save to the file.</param>
		/// <returns>ReturnCode.[InvalidPath,Success,Unauthorized,UnkownError]</returns>
		public static ReturnCode SaveToFile(string filePath, string content)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath) || !filePath.StartsWith(BrowserAppDataPath))
					return ReturnCode.InvalidPath;

				// ensure the directory exists
				var directory = Path.GetDirectoryName(filePath);
				if (directory is not null && !Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				File.WriteAllText(filePath, content);
				return ReturnCode.Success;
			}
			catch (UnauthorizedAccessException)
			{
				return ReturnCode.Unauthorized;
			}
			catch (Exception)
			{
				return ReturnCode.UnknownError;
			}
		}

		/// <summary>
		/// Saves plain text content to a file at the specified path.
		/// Note that it is recommended to use the BuildFilePath method to get the path.
		/// </summary>
		/// <param name="filePath">path to the location to save to.</param>
		/// <param name="content">plain text content to save to the file.</param>
		/// <returns>a Task containing: ReturnCode.[InvalidPath,Success,Unauthorized,UnkownError]</returns>
		public static async Task<ReturnCode> SaveToFileAsync(string filePath, string content)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath) || !filePath.StartsWith(BrowserAppDataPath))
					return ReturnCode.InvalidPath;

				var directory = Path.GetDirectoryName(filePath);
				if (directory is not null && !Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				await File.WriteAllTextAsync(filePath, content);
				return ReturnCode.Success;
			}
			catch (UnauthorizedAccessException)
			{
				return ReturnCode.Unauthorized;
			}
			catch (Exception)
			{
				return ReturnCode.UnknownError;
			}
		}
		
		/// <summary>
		/// Saves a class as json content to a file at the specified path.
		/// Note that it is recommended to use the BuildFilePath method to get the path.
		/// Also note that this method is synchronous, but there is an asynchronous version available.
		/// </summary>
		/// <param name="filePath">path to the location to save to.</param>
		/// <param name="content">an object of a class to save to the file.</param>
		/// <returns>ReturnCode.[InvalidPath,Success,Unauthorized,UnkownError]</returns>
		public static ReturnCode SaveToFile<T>(string filePath, T content) where T : class
		{
			if (!filePath.EndsWith(".json"))
				return ReturnCode.InvalidPath;
			
			var jsonContent = JsonSerializer.Serialize(content);
			return SaveToFile(filePath, jsonContent);
		}
		
		/// <summary>
		/// Saves a class as json content to a file at the specified path.
		/// Note that it is recommended to use the BuildFilePath method to get the path.
		/// </summary>
		/// <param name="filePath">path to the location to save to.</param>
		/// <param name="content">an object of a class to save to the file.</param>
		/// <typeparam name="T">The type of the class to save serializes the given object asas.</typeparam>
		/// <returns>a Task containing: ReturnCode.[InvalidPath,Success,Unauthorized,UnkownError]</returns>
		public static async Task<ReturnCode> SaveToFileAsync<T>(string filePath, T content) where T : class
		{
			if (!filePath.EndsWith(".json"))
				return ReturnCode.InvalidPath;
			
			var jsonContent = JsonSerializer.Serialize(content);
			return await SaveToFileAsync(filePath, jsonContent);
		}
		#endregion

		#region DeleteFile
		/// <summary>
		/// Deletes a file at the specified path.
		/// Like normal, only files within the app's data directory can be deleted.
		/// Note that it is recommended to use the BuildFilePath method to get the path.
		/// Also note that this method is synchronous, but there is an asynchronous version available.
		/// </summary>
		/// <param name="filePath">path to the file to delete</param>
		/// <returns>ReturnCode.[InvalidPath,NotFound,Success,Unauthorized,UnknownError]</returns>
		public static ReturnCode DeleteFile(string filePath)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath) || !filePath.StartsWith(BrowserAppDataPath))
					return ReturnCode.InvalidPath;
				
				if (!File.Exists(filePath))
					return ReturnCode.NotFound;
				
				File.Delete(filePath);
				
				return ReturnCode.Success;
			}
			catch (UnauthorizedAccessException)
			{
				return ReturnCode.Unauthorized;
			}
			catch (Exception)
			{
				return ReturnCode.UnknownError;
			}
		}
		/// <summary>
		/// Deletes a file at the specified path.
		/// Like normal, only files within the app's data directory can be deleted.
		/// Note that it is recommended to use the BuildFilePath method to get the path.
		/// Also note that this method is asynchronous, but File.Delete is not, so it wraps the synchronous method in a Task.
		/// </summary>
		/// <param name="filePath">path to the file to delete</param>
		/// <returns>A Task containing: ReturnCode.[InvalidPath,NotFound,Success,Unauthorized,UnknownError]</returns>
		public static async Task<ReturnCode> DeleteFileAsync(string filePath)
		{
			return await Task.Run(() => DeleteFile(filePath));
		}
		#endregion
	#endregion

	#region Miscellaneous
	public static async Task<ReturnCode> InitialFolderSetupAsync()
	{
		var code = await CreateFolderAsync(BrowserAppDataPath);
		if (code != ReturnCode.Success)
			return code;

		var returnCodes = (await Task.WhenAll(
			CreateFolderAsync(InstanceFolderPath),
			CreateFolderAsync(BrowserDataFolderPath),
			CreateFolderAsync(CacheFolderPath)
		)).ToList();
		
		if (returnCodes.Any(rc => rc != ReturnCode.Success && rc != ReturnCode.AlreadyExists))
			return returnCodes.First(rc => rc != ReturnCode.Success && rc != ReturnCode.AlreadyExists);

		return ReturnCode.Success;
	}
	
	public static async Task<ReturnCode> ExportDataAsync(string exportPath)
	{
		throw new NotImplementedException(); // used to see data and copy and paste to another computer.
	}
	
	public static async Task<ReturnCode> ImportDataAsync(string importPath)
	{
		throw new NotImplementedException();
	}
	
	public static ReturnCode AttemptFunctionWithRetry(Func<ReturnCode> function, int maxRetries = 5)
	{
		var attempts = 0;
		ReturnCode result;

		do
		{
			result = function();
			attempts++;
		} while (result is not (ReturnCode.Success or ReturnCode.InvalidPath or ReturnCode.AlreadyExists)
		         && attempts < maxRetries);

		return result;
	}
	
	public static (ReturnCode code, T? content) AttemptFunctionWithRetry<T>(Func<(ReturnCode code, T? content)> function, int maxRetries = 5)
	{
		var attempts = 0;
		(ReturnCode code, T? content) result;

		do
		{
			result = function();
			attempts++;
		} while (result.code is not (ReturnCode.Success or ReturnCode.InvalidPath or ReturnCode.AlreadyExists)
		         && attempts < maxRetries);

		return result;
	}
	
	public static async Task<ReturnCode> AttemptFunctionWithRetryAsync(Func<Task<ReturnCode>> function, int maxRetries = 5)
	{
		var attempts = 0;
		ReturnCode result;

		do
		{
			result = await function();
			attempts++;
		} while (result is not (ReturnCode.Success or ReturnCode.InvalidPath or ReturnCode.AlreadyExists)
		         && attempts < maxRetries);

		return result;
	}
	
	public static async Task<(ReturnCode code, T? content)> AttemptFunctionWithRetryAsync<T>(Func<Task<(ReturnCode code, T? content)>> function, int maxRetries = 5)
	{
		var attempts = 0;
		(ReturnCode code, T? content) result;

		do
		{
			result = await function();
			attempts++;
		} while (result.code is not (ReturnCode.Success or ReturnCode.InvalidPath or ReturnCode.AlreadyExists)
		         && attempts < maxRetries);

		return result;
	}
	
	#endregion
}