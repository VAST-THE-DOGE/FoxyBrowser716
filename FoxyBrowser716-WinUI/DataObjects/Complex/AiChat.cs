using System.Threading;
using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataManagement;
using Mistral.SDK;
using Mistral.SDK.Common;
using Mistral.SDK.DTOs;
using Tool = Mistral.SDK.Common.Tool;

namespace FoxyBrowser716_WinUI.DataObjects.Complex;

public class AiChat
{
	private static int _chatCounter;
	public int Id { get; private set; }

	
	#region Prompts
	const string GenericPrompt = 
@"# Browser AI Assistant - Development Mode

You are a helpful browser assistant that can answer questions and provide information through a two-phase process: thinking and responding.

## Core Behavior
- Provide accurate, helpful responses to user questions
- Be conversational and friendly while remaining focused
- Ask clarifying questions when user requests are unclear or incomplete
- Explain your reasoning when appropriate

## Two-Phase Process
### Phase 1: Thinking
When you receive a ""Thinking..."" system message, you are in the thinking phase where you can:
- Use available functions to gather information or perform actions
- Make multiple function calls as needed
- Plan your approach to the user's request
- When finished with all necessary function calls, use `end_thinking()` to proceed to the response phase

### Phase 2: Responding  
After the thinking phase ends, provide your final response to the user based on the information gathered and actions performed during thinking.

## Available Functions (Thinking Phase Only)
You have access to browser automation functions that allow you to:
- Navigate tabs to URLs or perform searches
- Get information about open browser tabs
- End the thinking phase when ready to respond
- More capabilities will be available as development continues

## Anti-Hallucination Guidelines
- Only provide information you are confident about
- When uncertain, clearly state ""I'm not sure about this"" or ""I don't have reliable information on this""
- Avoid making up specific facts, statistics, URLs, or technical details
- Use function calls during thinking phase to gather real information rather than guessing
- Base your final response on actual function results, not assumptions

## Response Style
- Keep responses concise but complete
- Use natural language, avoid overly formal tone
- Break down complex topics into digestible parts
- Prioritize actionable information when relevant
- Reference actions you took during the thinking phase when relevant to the user

## Process Flow Example
1. User asks a question
2. System: ""Thinking...""
3. You use functions as needed to gather information, but only use functions wher necessary.
4. You call `end_thinking()` when ready
5. System: ""Finished Thinking.""
6. You provide final response based on thinking phase results

Remember: It's better to admit uncertainty than to provide potentially incorrect information.";
	#endregion
	
	public DateTime creationTime { get; } = DateTime.Now;
	private List<string> _chatLog = [];
	public ReadOnlyCollection<string> MessageLog => _chatLog.AsReadOnly();

	public string? Error;
	
	private MistralClient _client;
	private MainWindow _mainWindow;
	private List<ChatMessage> _messages;
	public ReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();
	
	public AiChat(MistralClient client, MainWindow mainWindow, List<ChatMessage>? messages = null)
	{
		Id = Interlocked.Increment(ref _chatCounter);
		_mainWindow	= mainWindow;
		_client = client;
		_messages = messages ?? [
			new ChatMessage(ChatMessage.RoleEnum.System, GenericPrompt),
		];
	}

	public async Task UserRequest(string message, Action<string?> onChunkReceived/*, bool braveMode = true TODO*/)
	{
		_chatLog.Clear();
		
		_chatLog.Add("Added message from user to history");
		
		_messages.Add(new ChatMessage(ChatMessage.RoleEnum.User, message));
		var thinking = true;
		
		var tools = new List<Tool>
		{
			Tool.FromFunc("navigate_tab_to_url",
				async ([FunctionParameter("tab_id (int)", true)] int tab_id,
					[FunctionParameter("url (string)", true)] string url) =>
				{
					if (_mainWindow.TabManager.TryGetTab(tab_id, out var tab))
					{
						await tab!.NavigateOrSearch(url);
						return $"tab with id '{tab_id}' navigated to or searched for '{url}'";
					}
					else
					{
						return $"no tab with id '{tab_id}' found, use 'get_list_of_tabs' to get a list of all tabs to verify this id";
					}

				},
				"tries to navigate a tab with the id given to a specific url that is given. If the given url is not a valid website, it will search for it instead using the default search engine."),
			Tool.FromFunc("get_list_of_tabs", async () =>
				{
					var tabs = _mainWindow.TabManager.GetAllTabs();
					var activeTab = _mainWindow.TabManager.ActiveTabId;
					var sb = new StringBuilder();

					sb.AppendLine($"tabs:");
					foreach (var tab in tabs)
					{
						sb.AppendLine($"Tab {tab.Key}: '{tab.Value.Info.Title}' at '{tab.Value.Info.Url}' {(tab.Key == activeTab ? "(ACTIVE)" : "")}");
						
					}

					return sb.ToString();
				},
				"Gets a list of all tabs with some basic info about them."),
			Tool.FromFunc("end_thinking",
				async () =>
				{
					thinking = false;
					return "thinking ended.";
				},
				"ends the thinking process which is where you can do function calls."),
		};

		//var systemThink = new ChatMessage(ChatMessage.RoleEnum.System, "Thinking (to exit, call the function 'end_thinking()' without any arguments)...");
		//_messages.Add(systemThink);

		_chatLog.Add("Entering thinking phase");

		
		const int timeout = 10;
		var i = 0;
		while (thinking && i++ < timeout)
		{
			var thinkingRequest = new ChatCompletionRequest(
				ModelDefinitions.MistralSmall,
				_messages,
				temperature: 0.1m)
			{
				ToolChoice = ToolChoiceType.Any,
				Tools = tools,
				MaxTokens = 512,
			};
			_chatLog.Add($"Think request {i} made. Sending...");
			var thinkingResponse = await _client.Completions.GetCompletionAsync(thinkingRequest);
			if (thinkingResponse.ToolCalls.Count > 0)
			{
				_messages.Add(thinkingResponse.Choices.First().Message);
				_chatLog.Add($"Adding think response {i} to history.");
			}
			else
				_chatLog.Add($"skipped think response {i}, no tool calls.");
			
			//systemThink.Content = $"{systemThink.Content}\nCalled {thinkingResponse.ToolCalls.Count} Tools with Thought \"{(string.IsNullOrWhiteSpace(systemThink.Content) ? "None" : systemThink.Content)}\"";
			foreach (var toolCall in thinkingResponse.ToolCalls)
			{
				_chatLog.Add($"Calling tool: `{toolCall.Name}({string.Join(", ", toolCall.Arguments)})`");
				var result = await toolCall.Invoke<Task<string>>();
				_chatLog.Add($"Tool `{toolCall.Name}` returned: ` {result} ` ");
				_messages.Add(new ChatMessage(toolCall, result));
				_chatLog.Add($"Adding result of tool {toolCall.Name} to history.");
			}
		}
		
		_chatLog.Add($"Finished thinking phase.");
		
		//_messages.Add(new ChatMessage(ChatMessage.RoleEnum.System, i < timeout ? "Finished Thinking." : "Thinking Timed Out."));

		var request = new ChatCompletionRequest(
			ModelDefinitions.MistralSmall,
			_messages,
			temperature: 0.5m)
		{
			MaxTokens = 2048
		};
		
		_chatLog.Add("Sending final request...");
		
		var firstChunk = true;
		
		var response = new StringBuilder();
		await foreach (var chunk in _client.Completions.StreamCompletionAsync(request))
		{
			if (firstChunk)
			{
				_chatLog.Add("Received first chunk of response.");
				firstChunk = false;
			}
			
			if (chunk.Choices?.FirstOrDefault()?.Delta?.Content != null)
			{
				var content = chunk.Choices.First().Delta.Content;
				response.Append(content);
				onChunkReceived?.Invoke(content);
			}
		}
		_chatLog.Add("Received final chunk of response.");
		onChunkReceived?.Invoke(null); // null to indicate the end of response
		_messages.Add(new ChatMessage(ChatMessage.RoleEnum.Assistant, response.ToString()));
		_chatLog.Add("Added response to history.");
	}
}
