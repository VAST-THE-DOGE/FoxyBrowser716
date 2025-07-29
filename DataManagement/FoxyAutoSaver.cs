using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;

namespace FoxyBrowser716_WinUI.DataManagement;

public abstract class NotifyPropertyChanged : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public abstract class IFoxyAutoSaverItem
{
	internal event Action<IFoxyAutoSaverItem, SavePriority>? SaveRequested;
	
	private SavePriority Priority { get; init; } = SavePriority.Normal;
	private string FilePath { get; init; }
	internal abstract Task Save();
	internal abstract Task Load();

	public virtual void RequestSave(SavePriority? priority)
	{
		SaveRequested?.Invoke(this, priority ?? Priority);
	}
	public abstract void RequestLoad();
}

/// <summary>
/// TODO: make better summary.
///
/// This should act as the field itself! pass in new T()
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class FoxyAutoSaverField<T> : IFoxyAutoSaverItem where T : NotifyPropertyChanged
{
	private SavePriority Priority { get; }
	private string FilePath { get; init; }
	public bool IsLoaded { get; private set; } = false;
	public T? Item { get; private set; }
	private Func<T> ItemFactory { get; init; }
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="itemFactory"></param>
	/// <param name="fileName"></param>
	/// <param name="folderType"></param>
	/// <param name="instanceName"></param>
	/// <param name="priority"></param>
	public FoxyAutoSaverField(Func<T> itemFactory, string fileName, FoxyFileManager.FolderType folderType, string? instanceName = null, SavePriority priority = SavePriority.Normal)
	{
		ItemFactory = itemFactory;
		FilePath = FoxyFileManager.BuildFilePath(fileName, folderType, instanceName);
		Priority = priority;
	}
	
	internal override async Task Save()
	{
		if (!IsLoaded) return;
		if (Item is null) return;
		
		var result = await FoxyFileManager.SaveToFileAsync(FilePath, Item);
		
		if (result != FoxyFileManager.ReturnCode.Success)
			throw new Exception($"Failed to save {FilePath}: {result}");
	}
	internal override async Task Load()
	{
		var result = await FoxyFileManager.ReadFromFileAsync<T>(FilePath);

		if (result.code == FoxyFileManager.ReturnCode.NotFound)
		{
			Item = ItemFactory();
			Item.PropertyChanged += HandlePropertyChanged;
			IsLoaded = true;
			return;
		}
		
		if (result.code != FoxyFileManager.ReturnCode.Success || result.content is null)
			throw new Exception($"Failed to save {FilePath}: {result}");
		
		Item = result.content;
		Item.PropertyChanged += HandlePropertyChanged;
		IsLoaded = true;
	}

	private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (IsLoaded)
		{
			RequestSave(Priority);
		}
	}
}

public abstract class FoxyAutoSaverList<T> : IFoxyAutoSaverItem where T : NotifyPropertyChanged
{
	private SavePriority Priority { get; init; } = SavePriority.Normal;
	protected string FilePath { get; init; }
	public bool IsLoaded { get; private set; } = false;
	
	public ObservableCollection<T>? Items { get; private set; } = null;
	
	private readonly HashSet<NotifyPropertyChanged> _subscribedItems = [];

	public FoxyAutoSaverList(ObservableCollection<T> item, string fileName, FoxyFileManager.FolderType folderType, string? instanceName = null, SavePriority priority = SavePriority.Normal)
	{
		Items = item;
		FilePath = FoxyFileManager.BuildFilePath(fileName, folderType, instanceName);
		Priority = priority;
	}
	
	internal override async Task Save()
	{
		if (!IsLoaded) return;
		if (Items is null) return;
		
		var result = await FoxyFileManager.SaveToFileAsync(FilePath, Items);
		
		if (result != FoxyFileManager.ReturnCode.Success)
			throw new Exception($"Failed to save {FilePath}: {result}");
	}
	internal override async Task Load()
	{
		var result = await FoxyFileManager.ReadFromFileAsync<T[]>(FilePath);
		Items = [];
		
		if (result.code == FoxyFileManager.ReturnCode.NotFound)
		{
			Items.CollectionChanged += HandleCollectionChanged;
			IsLoaded = true;
			return;
		}
		
		if (result.code != FoxyFileManager.ReturnCode.Success || result.content is null)
			throw new Exception($"Failed to save {FilePath}: {result}");

		foreach (var item in result.content)
		{
			Items.Add(item);
		}
		Items.CollectionChanged += HandleCollectionChanged;
		IsLoaded = true;
	}

	private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.NewItems is not null)
		{
			foreach (NotifyPropertyChanged item in e.NewItems)
			{
				if (_subscribedItems.Add(item))
				{
					item.PropertyChanged += HandlePropertyChanged;
				}
			}
		}
    
		if (e.OldItems is not null && e.Action != NotifyCollectionChangedAction.Move)
		{
			foreach (NotifyPropertyChanged item in e.OldItems)
			{
				if (_subscribedItems.Remove(item))
				{
					item.PropertyChanged -= HandlePropertyChanged;
				}
			}
		}
    
		if (e.Action == NotifyCollectionChangedAction.Reset)
		{
			foreach (var item in _subscribedItems)
			{
				item.PropertyChanged -= HandlePropertyChanged;
			}
			_subscribedItems.Clear();
        
			if (Items is not null)
			{
				foreach (var item in Items)
				{
					if (_subscribedItems.Add(item))
					{
						item.PropertyChanged += HandlePropertyChanged;
					}
				}
			}
		}
    
		if (IsLoaded)
		{
			RequestSave(Priority);
		}
	}
	
	private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (IsLoaded)
		{
			RequestSave(Priority);
		}
	}
}

public enum SavePriority
{
	Low,
	Normal,
	High,
	Immediate
}

public class FoxyAutoSaver : IDisposable
{
	private uint _saveIntervalMs = 15000;
	public uint SaveIntervalMs
	{
		get => _saveIntervalMs;
		set
		{
			_saveIntervalMs = value;
			_queueTimer.Interval = value;
		}
	}

	private bool _runningTick = false;
	// no new objects to save while saving.
	private readonly ConcurrentQueue<(IFoxyAutoSaverItem item, SavePriority priority)> _waitQueue = [];
	
	private readonly ConcurrentQueue<IFoxyAutoSaverItem> _lowQueue = [];
	private readonly ConcurrentQueue<IFoxyAutoSaverItem> _normalQueue = [];
	private readonly ConcurrentQueue<IFoxyAutoSaverItem> _highQueue = [];

	private readonly Dictionary<IFoxyAutoSaverItem, SavePriority> _queuedItems = [];
	
	private readonly Timer _queueTimer = new();
	
	private readonly HashSet<IFoxyAutoSaverItem> _items = [];
	
	public FoxyAutoSaver(List<IFoxyAutoSaverItem> items)
	{
		_queueTimer.Interval = SaveIntervalMs;
		_queueTimer.Elapsed += HandleQueueTimerElapsed;
		
		foreach (var item in items)
			if (_items.Add(item))
				item.SaveRequested += AddToQueue;
	}

	public async Task<bool> AddItem(IFoxyAutoSaverItem item, bool loadItem = true)
	{
		if (!_items.Add(item)) return false;
		
		if (loadItem)
		{
			await item.Load();
		}
		
		item.SaveRequested += AddToQueue;
		return true;
	}
	
	public bool RemoveItem(IFoxyAutoSaverItem item)
	{
		if (!_items.Remove(item)) return false;
		
		item.SaveRequested -= AddToQueue;
		return true;
	}
	
	public async Task Start(bool LoadItems = true)
	{
		if (LoadItems)
		{
			foreach (var item in _items)
			{
				await item.Load();
			}
		}

		_queueTimer.Start();
	}

	private uint tick;
	private async void HandleQueueTimerElapsed(object? sender, ElapsedEventArgs e)
	{
		if (_runningTick)
			return;
		
		_runningTick = true;
		List<Task> tasks = [];
		switch (tick++)
		{
			case 3:
				tasks.Add(SaveQueue(_lowQueue, SavePriority.Low));
				goto case 1;
			case 1:
				tasks.Add(SaveQueue(_normalQueue, SavePriority.Normal));
				goto case 0;
			case 0:
			case 2:
				tasks.Add(SaveQueue(_highQueue, SavePriority.High));
				tick = 0;
				await Task.WhenAll(tasks);
				break;
		}

		while (_waitQueue.TryDequeue(out var pair))
		{
			switch (pair.priority)
			{
				case SavePriority.Low:
					_lowQueue.Enqueue(pair.item);
					break;
				case SavePriority.Normal:
					_normalQueue.Enqueue(pair.item);
					break;
				case SavePriority.High:
					_highQueue.Enqueue(pair.item);
					break;
				
				// Should never happen, but just in case to prevent item not saving and memory leaks.
				case SavePriority.Immediate:
					tasks.Add(pair.item.Save());
					_queuedItems.Remove(pair.item);
					break;
			}
		}

		_runningTick = false;
	}
	
	private void AddToQueue(IFoxyAutoSaverItem item, SavePriority priority)
	{
		if (_runningTick)
		{
			_waitQueue.Enqueue((item, priority));
			return;
		}

		if (_queuedItems.TryGetValue(item, out var oldPriority) && oldPriority >= priority)
			return;
		
		switch (priority)
		{
			case SavePriority.Low:
				_lowQueue.Enqueue(item);
				break;
			case SavePriority.Normal:
				_normalQueue.Enqueue(item);
				break;
			case SavePriority.High:
				_highQueue.Enqueue(item);
				break;
			case SavePriority.Immediate:
				item.Save();
				return;
		}

		_queuedItems[item] = priority;
	}
	
	private async Task SaveQueue(ConcurrentQueue<IFoxyAutoSaverItem> queue, SavePriority queuePriority)
	{
		List<Task> tasks = [];
		while (queue.TryDequeue(out var item))
		{
			if (_queuedItems.TryGetValue(item, out var priority) && priority == queuePriority)
			{
				tasks.Add(item.Save());
				_queuedItems.Remove(item);
			}
		}
		await Task.WhenAll(tasks);
	}

	public void Dispose()
	{
		_queueTimer?.Dispose();
		foreach (var item in _items)
		{
			item.SaveRequested -= AddToQueue;
		}
		_items.Clear();
	}
} 