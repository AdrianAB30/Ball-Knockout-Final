using System;
using UnityEngine;

[Serializable]
public class ChatMessage
{
    public string SenderName;
    public string MessageText;
    public ChatMessageType MessageType;
    public string TargetPlayerId;

    public ChatMessage() { }

    public ChatMessage(string senderName, string messageText, ChatMessageType messageType = ChatMessageType.Global, string targetPlayerId = null)
    {
        SenderName = senderName;
        MessageText = messageText;
        MessageType = messageType;
        TargetPlayerId = targetPlayerId;
    }

    public string GetFormattedMessage(string currentPlayerId)
    {
        switch (MessageType)
        {
            case ChatMessageType.Global:
                return $"[All] {SenderName}: {MessageText}";

            case ChatMessageType.Private:
                if (TargetPlayerId == currentPlayerId)
                {
                    return $"[Private from {SenderName}]: {MessageText}";
                }
                else if (SenderName == PlayerAccountManager.Instance.PlayerName)
                {
                    string targetName = "Unknown";
                    if (LobbyManager.Instance != null)
                    {
                        targetName = LobbyManager.Instance.GetPlayerNameById(TargetPlayerId);
                    }
                    return $"[Private to {targetName}]: {MessageText}";
                }
                return null;

            default:
                return $"[System] {MessageText}";
        }
    }
}

public enum ChatMessageType
{
    Global,
    Private,
    System
}