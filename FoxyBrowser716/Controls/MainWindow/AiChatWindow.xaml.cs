using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Text;
using ColorCode.Styling;
using CommunityToolkit.WinUI.Animations;
using CommunityToolkit.WinUI.UI.Controls;
using FoxyBrowser716.Controls.Generic;
using FoxyBrowser716.Controls.HomePage;
using FoxyBrowser716.DataManagement;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.DataObjects.Complex;
using Material.Icons;
using Material.Icons.WinUI3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Mistral.SDK.DTOs;
using Style = ColorCode.Styling.Style;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716.Controls.MainWindow;

public sealed partial class AiChatWindow : UserControl
{
    private AiHandler _aiHandler;
    private MainWindow _mainWindow;
    private bool _hasError = false;
    
    public event Action CloseRequested;
    
    public AiChatWindow()
    {
        InitializeComponent();
        ApplyTheme();
    }

    public async Task Initialize(MainWindow mainWindow)
    {
        _aiHandler = await AiHandler.New(mainWindow);
        _mainWindow = mainWindow;
        TitleText.Text = "New Chat";
    }
    
    internal Theme CurrentTheme { get; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        Root.BorderBrush = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor);
        Root.Background = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);

        TitleText.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        MText.Foreground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor);

        Input.CurrentTheme = CurrentTheme;
        ButtonBraveMode.CurrentTheme = CurrentTheme;
        ButtonSend.CurrentTheme = CurrentTheme;
        ButtonHistory.CurrentTheme = CurrentTheme;
        ButtonClose.CurrentTheme = CurrentTheme with
        {
            SecondaryForegroundColor = CurrentTheme.NoColor,
            PrimaryHighlightColor = CurrentTheme.NoColor
        };
        ButtonNewChat.CurrentTheme = CurrentTheme;
    }

    private async Task LoadChatMessages(AiChat chat)
    {
        MessageList.Children.Clear();
        
        // Check if chat has an error
        if (!string.IsNullOrEmpty(chat.Error))
        {
            _hasError = true;
            var errorBubble = GetMessageBubble(new ChatMessage(ChatMessage.RoleEnum.Assistant, chat.Error));
            errorBubble.Item1.Background = new SolidColorBrush(CurrentTheme.NoColor);
            MessageList.Children.Add(errorBubble.Item1);
            
            // Disable input when there's an error
            Input.Visibility = Visibility.Collapsed;
            ButtonBraveMode.Visibility = Visibility.Collapsed;
            ButtonSend.Visibility = Visibility.Collapsed;
            MText.Visibility = Visibility.Collapsed;
        }
        else
        {
            _hasError = false;
            
            if (chat?.Messages?.Count > 0)
            {
                MText.Visibility = Visibility.Collapsed;
                foreach (var message in chat.Messages.Where(m => m.Role is ChatMessage.RoleEnum.User or ChatMessage.RoleEnum.Assistant && m.Content != ""))
                {
                    MessageList.Children.Add(GetMessageBubble(message).Item1);
                }
            }
            else
            {
                MText.Visibility = Visibility.Visible;
            }
            
            // Enable input when no error
            Input.Visibility = Visibility.Visible;
            ButtonBraveMode.Visibility = Visibility.Visible;
            ButtonSend.Visibility = Visibility.Visible;
        }
    }

    private bool inHistory = false;
    private async void ButtonHistory_OnClick(object sender, RoutedEventArgs e)
    {
        if (inHistory)
        {
            var chat = _aiHandler.ActiveChat;
            await LoadChatMessages(chat);
            
            ButtonNewChat.Visibility = Visibility.Collapsed;
            inHistory = false;
        }
        else
        {
            var history = _aiHandler.Chats.Values.OrderBy(c => c.creationTime);
            MessageList.Children.Clear();
            
            foreach (var chat in history)
            {
                var chatCard = new Grid()
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star)},
                        new ColumnDefinition { Width = GridLength.Auto},
                    },
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto},
                        new RowDefinition { Height = GridLength.Auto},
                    },
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5),
                    CornerRadius = new CornerRadius(5),
                    Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColor),
                };
                
                var buttonRemove = new FIconButton()
                {
                    Content = new MaterialIcon() {Kind = MaterialIconKind.Delete},
                    CurrentTheme = CurrentTheme,
                    Width = 26,
                    Height = 26,
                };
                buttonRemove.OnClick += (_, _) =>
                {
                    _ = _aiHandler.DeleteChat(chat.Id);
                    MessageList.Children.Remove(chatCard);
                };

                var title = new TextBlock()
                {
                    Text = $"Chat #{chat.Id}",
                    FontSize = 20,
                    Height = 26,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor),
                    Margin = new Thickness(5, 5, 5, 0),
                };
                
                var date = new TextBlock()
                {
                    Text = chat.creationTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    FontSize = 14,
                    Foreground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor),
                    Margin = new Thickness(5, 0, 5, 5),
                };
                
                chatCard.Children.Add(title);
                chatCard.Children.Add(buttonRemove);
                chatCard.Children.Add(date);
                
                Grid.SetColumn(title, 0);
                Grid.SetRow(title, 0);
                
                Grid.SetColumn(buttonRemove, 1);
                Grid.SetRow(buttonRemove, 0);
                
                Grid.SetColumn(date, 0);
                Grid.SetRow(date, 1);

                chatCard.PointerPressed += async (_, e) =>
                {
                    if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
                    
                    if (buttonRemove.PointerOver) return;
                    await _aiHandler.SwapChat(chat.Id);
                    await LoadChatMessages(chat);
                    
                    inHistory = false;
                    ButtonNewChat.Visibility = Visibility.Collapsed;
                };
                
                MessageList.Children.Add(chatCard);
            }
            
            MText.Visibility = Visibility.Collapsed;
            ButtonNewChat.Visibility = Visibility.Visible;
            ButtonSend.Visibility = Visibility.Collapsed;
            ButtonBraveMode.Visibility = Visibility.Collapsed;
            Input.Visibility = Visibility.Collapsed;
            inHistory = true;
        }
    }
    
    internal void ButtonClose_OnClick(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
        CloseRequested?.Invoke();
    }
    
    private void ButtonBraveMode_OnClick(object sender, RoutedEventArgs e)
    {
        //throw new NotImplementedException();
    }
    
    private void ButtonSend_OnClick(object sender, RoutedEventArgs e)
    {
        Input_OnEnterPressed();
    }

    private async void Input_OnEnterPressed()
    {
        // Don't allow input if there's an error
        if (_hasError) return;
        
        var input = Input.Text;
        if (string.IsNullOrWhiteSpace(input)) return;
        Input.Text = "";
        
        var userBubble = GetMessageBubble(new ChatMessage(ChatMessage.RoleEnum.User, input));
        MessageList.Children.Add(userBubble.Item1);

        AiChat chat = _aiHandler.ActiveChat;

        if (chat == null)
        {
            await _aiHandler.SwapChat(-1);
            chat = _aiHandler.ActiveChat!;
            TitleText.Text = $"Chat #{chat.Id}";
            MText.Visibility = Visibility.Collapsed;
        }

        var aiBubble = GetMessageBubble(new ChatMessage(ChatMessage.RoleEnum.Assistant, ""));
        MessageList.Children.Add(aiBubble.Item1);

        Input.Visibility = Visibility.Collapsed;
        ButtonBraveMode.Visibility = Visibility.Collapsed;
        ButtonSend.Visibility = Visibility.Collapsed;

        _ = Task.Run(async () =>
        {
            try
            {
                await chat.UserRequest(input, response =>
                {
                    AppServer.UiDispatcherQueue.TryEnqueue(() =>
                    {

                        if (response is not null) aiBubble.Item2.Text += response;
                        else
                        {
                            Input.Visibility = Visibility.Visible;
                            ButtonBraveMode.Visibility = Visibility.Visible;
                            ButtonSend.Visibility = Visibility.Visible;
                            Scroller.ChangeView(null, Scroller.ScrollableHeight, null);
                        }
                    });
                });
            }
            catch (Exception e)
            {
                AppServer.UiDispatcherQueue.TryEnqueue(() =>
                {
                    var i = 1;
                    var log = _aiHandler.ActiveChat!.MessageLog.ToList().Select(m => $"> {i++}. {m}");
                    var errorMessage = $"# Error: \n `` {e.Message} `` \n \n --- \n \n `` {e.StackTrace} `` \n \n --- \n \n  Log:  \n \n {string.Join($"\n", log)} ";
                    aiBubble.Item2.Text = errorMessage;
                    _aiHandler.ActiveChat.Error = errorMessage;
                    aiBubble.Item1.Background = new SolidColorBrush(CurrentTheme.NoColor);
                    
                    // Set error state and disable input
                    _hasError = true;
                    Input.Visibility = Visibility.Collapsed;
                    ButtonBraveMode.Visibility = Visibility.Collapsed;
                    ButtonSend.Visibility = Visibility.Collapsed;
                    
                    Scroller.ChangeView(null, Scroller.ScrollableHeight, null);
                });
            }
        });
    }

    private (Border, MarkdownTextBlock) GetMessageBubble(ChatMessage message)
    {
        var textBlock = new MarkdownTextBlock
        {
            Text = message.Content,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor),
            Margin = new Thickness(5), 
            Background = new SolidColorBrush(Colors.Transparent),
            
            // Code block styling
            CodeBackground = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColor),
            CodeBorderBrush = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor),
            CodeBorderThickness = new Thickness(1),
            CodeForeground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor),
            CodeFontFamily = new FontFamily("Consolas"),
            CodeMargin = new Thickness(0, 8, 0, 8),
            CodePadding = new Thickness(12),
            
            // Inline code styling
            InlineCodeBackground = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColor),
            InlineCodeBorderBrush = new SolidColorBrush(CurrentTheme.SecondaryHighlightColor),
            InlineCodeForeground = new SolidColorBrush(CurrentTheme.PrimaryAccentColor),
            InlineCodePadding = new Thickness(4, 2, 4, 2),
            InlineCodeMargin = new Thickness(2, 0, 2, 0),
            
            // Header styling
            Header1Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor),
            Header2Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor),
            Header3Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor),
            Header4Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor),
            Header5Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor),
            Header6Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor),
            
            // Quote styling
            QuoteBackground = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent),
            QuoteBorderBrush = new SolidColorBrush(CurrentTheme.PrimaryHighlightColor),
            QuoteForeground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor),
            QuoteBorderThickness = new Thickness(4, 0, 0, 0),
            QuoteMargin = new Thickness(0, 8, 0, 8),
            QuotePadding = new Thickness(12, 8, 12, 8),
            
            // Link styling
            LinkForeground = new SolidColorBrush(CurrentTheme.PrimaryAccentColor),
            
            // Table styling
            TableBorderBrush = new SolidColorBrush(CurrentTheme.SecondaryHighlightColor),
            TableCellPadding = new Thickness(8, 4, 8, 4),
            TableMargin = new Thickness(0, 8, 0, 8),
            
            // List styling
            ListMargin = new Thickness(0, 4, 0, 4),
            ListGutterWidth = 32,
            
            // Horizontal rule styling
            HorizontalRuleBrush = new SolidColorBrush(CurrentTheme.SecondaryHighlightColor),
            HorizontalRuleMargin = new Thickness(0, 16, 0, 16),
            HorizontalRuleThickness = 1,
            
            // Enable syntax highlighting
            UseSyntaxHighlighting = true,
            // CodeStyling = [
            //     new Style("") { },
            // ],
        };
        
        textBlock.LinkClicked += (_, e) =>
        {
            _mainWindow.TabManager.SwapActiveTabTo(_mainWindow.TabManager.AddTab(e.Link));
        };
        
        return (new Border
        {
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(message.Role == ChatMessage.RoleEnum.Assistant ? 50 : 5, 5,
                message.Role == ChatMessage.RoleEnum.User ? 50 : 5, 0),
            Child = textBlock,
            CornerRadius = new CornerRadius(10,message.Role == ChatMessage.RoleEnum.Assistant ? 0 : 10,
                10,message.Role == ChatMessage.RoleEnum.User ? 0 : 10),
            Background = message.Role == ChatMessage.RoleEnum.User ? 
                new SolidColorBrush(CurrentTheme.SecondaryHighlightColor) 
                : new SolidColorBrush(CurrentTheme.PrimaryBackgroundColor),
        }, textBlock);

    }

    private async void ButtonNewChat_OnClick(object sender, RoutedEventArgs e)
    {
        await _aiHandler.SwapChat(-1);
        TitleText.Text = $"Chat #{_aiHandler.ActiveChat!.Id}";
        
        await LoadChatMessages(_aiHandler.ActiveChat);
        
        inHistory = false;
        ButtonNewChat.Visibility = Visibility.Collapsed;
    }
}
