namespace FoxyBrowser716_WinUI.DataObjects;

public class AutosaveCollection<T>
{
	private static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true};
	
	private ObservableCollection<T> _list = [];

	private string _filePath;

	public Task OnLoaded;
	
	public AutosaveCollection(string filePath)
	{
		_filePath = filePath;
		_list.CollectionChanged += async (_, _) => await SaveCollection();
		OnLoaded = LoadCollection();
	}

	private async Task SaveCollection()
	{
		await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(_list, _serializerOptions));
	}
	
	private async Task LoadCollection()
	{
		if (File.Exists(_filePath))
			_list = JsonSerializer.Deserialize<ObservableCollection<T>?>(await File.ReadAllTextAsync(_filePath))??[];
	}

	public T this[int i] => _list[i];
}