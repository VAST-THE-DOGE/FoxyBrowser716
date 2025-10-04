using FoxyBrowser716.Controls.MainWindow;
using FoxyBrowser716.DataManagement;
using Mistral.SDK;

namespace FoxyBrowser716.DataObjects.Complex;

public class AiHandler
{
	private MainWindow _mainWindow;
	private MistralClient _client;
	private Dictionary<int, AiChat> _chats = [];
	public ReadOnlyDictionary<int, AiChat> Chats => _chats.AsReadOnly();
	
	public int ActiveChatId { get; private set; } = -1;
	public AiChat? ActiveChat => ActiveChatId == -1 ? null : _chats.GetValueOrDefault(ActiveChatId);
	
	private AiHandler() { }
	public static async Task<AiHandler> New(MainWindow mainWindow)
	{
		var ai = new AiHandler();
		await ai.Init(mainWindow);
		return ai;
	}
	private async Task Init(MainWindow mainWindow)
	{
		//TODO: load and save chat history
		
		var token = Environment.GetEnvironmentVariable("mistral_token");
		
		if (token is null)
			throw new Exception("Mistral token not found.");
		
		_client = new MistralClient(token);
		_mainWindow = mainWindow;
	}

	public async Task SwapChat(int id)
	{
		if (id == -1)
		{
			var chat = new AiChat(_client, _mainWindow);
			_chats.Add(chat.Id, chat);
			ActiveChatId = chat.Id;
		}
		else ActiveChatId = id;
	}

	public async Task DeleteChat(int id)
	{
		if (id == -1) return;
		_chats.Remove(id);
	}
}