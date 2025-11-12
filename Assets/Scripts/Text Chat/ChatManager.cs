using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;
using Unity.Services.Lobbies.Models; 

public class ChatManager : NonPersistentSingleton<ChatManager>
{
    public static event Action<ChatMessage> OnNewMessage;

    private List<ChatMessage> _chatHistory = new List<ChatMessage>();
    [SerializeField] private int maxChatMessages = 50;

    void Awake()
    {
        LobbyManager.OnChatMessageReceived += HandleLobbyChatMessage;
    }

    private void OnDestroy()
    {
        LobbyManager.OnChatMessageReceived -= HandleLobbyChatMessage;
    }

    private void HandleLobbyChatMessage(ChatMessage message)
    {
        _chatHistory.Add(message);
        if (_chatHistory.Count > maxChatMessages)
        {
            _chatHistory.RemoveAt(0);
        }
        OnNewMessage?.Invoke(message);
    }

    public async Task ProcessAndSendMessage(string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText)) return;

        string senderName = PlayerAccountManager.Instance.PlayerName;
        string senderId = AuthenticationService.Instance.PlayerId;

        if (inputText.StartsWith("/sendto "))
        {
            string command = inputText.Substring("/sendto ".Length);

            int firstQuote = command.IndexOf('"');
            int secondQuote = command.IndexOf('"', firstQuote + 1);

            if (firstQuote != -1 && secondQuote != -1)
            {
                string targetName = command.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                string messageContent = command.Substring(secondQuote + 1).Trim();

                if (string.IsNullOrWhiteSpace(messageContent)) return;

                string targetPlayerId = GetPlayerIdByName(targetName);

                if (!string.IsNullOrEmpty(targetPlayerId))
                {
                    ChatMessage privateMessage = new ChatMessage(senderName, messageContent, ChatMessageType.Private, targetPlayerId);
                    await LobbyManager.Instance.SendChatMessage(privateMessage);
                }
                else
                {
                    OnNewMessage?.Invoke(new ChatMessage("System", $"Player '{targetName}' not found in lobby.", ChatMessageType.System));
                }
            }
            else
            {
                OnNewMessage?.Invoke(new ChatMessage("System", "Private message format: /sendto \"PlayerName\" Your message here", ChatMessageType.System));
            }
        }
        else
        {
            ChatMessage globalMessage = new ChatMessage(senderName, inputText, ChatMessageType.Global);
            await LobbyManager.Instance.SendChatMessage(globalMessage);
        }
    }

    private string GetPlayerIdByName(string playerName)
    {
        if (LobbyManager.Instance == null || LobbyManager.Instance.JoinedLobby == null) return null;

        Player targetPlayer = LobbyManager.Instance.JoinedLobby.Players.FirstOrDefault(p =>
            p.Data != null &&
            p.Data.TryGetValue(LobbyManager.KEY_PLAYER_NAME, out PlayerDataObject nameData) &&
            nameData.Value.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        return targetPlayer?.Id;
    }
}