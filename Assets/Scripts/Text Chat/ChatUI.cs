using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Collections;
using Unity.Services.Vivox;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform chatContentContainer;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private ScrollRect chatScrollRect;

    private List<GameObject> _spawnedChatMessages = new List<GameObject>();
    private string _currentPlayerId;

    void Awake()
    {
        _currentPlayerId = AuthenticationService.Instance.PlayerId;
    }

    void OnEnable()
    {
        VivoxManager.OnMessageReceivedUI += DisplayNewMessage;

        LobbyManager.OnLobbyJoinedOrLeft += OnLobbyStateChanged;

        if (sendButton != null) sendButton.onClick.AddListener(OnSendButtonClicked);
        if (chatInputField != null) chatInputField.onSubmit.AddListener(OnInputSubmit);
    }

    void OnDisable()
    {
        VivoxManager.OnMessageReceivedUI -= DisplayNewMessage;

        LobbyManager.OnLobbyJoinedOrLeft -= OnLobbyStateChanged;
        ClearChatMessages();

        if (sendButton != null) sendButton.onClick.RemoveListener(OnSendButtonClicked);
        if (chatInputField != null) chatInputField.onSubmit.RemoveListener(OnInputSubmit);
    }

    private void OnLobbyStateChanged()
    {
        if (LobbyManager.Instance.JoinedLobby == null)
        {
            ClearChatMessages();
        }
    }

    private void OnSendButtonClicked()
    {
        SendMessageFromInput();
    }

    private void OnInputSubmit(string input)
    {
        SendMessageFromInput();
        chatInputField.ActivateInputField();
    }

    private async void SendMessageFromInput()
    {
        string message = chatInputField.text;
        if (string.IsNullOrWhiteSpace(message)) return;

        chatInputField.text = "";

        if (message.StartsWith("/sendto "))
        {
            string command = message.Substring("/sendto ".Length);
            int firstQuote = command.IndexOf('"');
            int secondQuote = command.IndexOf('"', firstQuote + 1);

            if (firstQuote != -1 && secondQuote != -1)
            {
                string targetName = command.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                string messageContent = command.Substring(secondQuote + 1).Trim();

                if (string.IsNullOrWhiteSpace(messageContent)) return;

                Debug.Log($"Enviando mensaje privado a {targetName}: {messageContent}");
                await VivoxManager.Instance.SendDirectMessage(messageContent, targetName);
            }
            else
            {
                Debug.LogWarning("Formato de mensaje privado incorrecto.");
            }
        }
        else
        {
            await VivoxManager.Instance.SendMessageToChannel(message, VivoxManager.Instance.CurrentTextChannel);
        }
    }

    private void DisplayNewMessage(ChatMessage message)
    {
        string localPlayerName = PlayerAccountManager.Instance.PlayerName;
        string formattedMessage;

        if (message.IsDirectMessage)
        {
            if (message.SenderDisplayName == localPlayerName)
            {
                formattedMessage = $"[Private to {message.RecipientDisplayName}]: {message.MessageText}";
            }
            else 
            {
                formattedMessage = $"[Private from {message.SenderDisplayName}]: {message.MessageText}";
            }
        }
        else 
        {
            formattedMessage = $"[All] {message.SenderDisplayName}: {message.MessageText}";
        }


        GameObject messageGO = Instantiate(chatMessagePrefab, chatContentContainer);
        TextMeshProUGUI messageText = messageGO.GetComponent<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = formattedMessage;
        }
        _spawnedChatMessages.Add(messageGO);

        if (_spawnedChatMessages.Count > 100)
        {
            Destroy(_spawnedChatMessages[0]);
            _spawnedChatMessages.RemoveAt(0);
        }

        StartCoroutine(ForceScrollDown());
    }

    private IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void ClearChatMessages()
    {
        foreach (GameObject msg in _spawnedChatMessages)
        {
            Destroy(msg);
        }
        _spawnedChatMessages.Clear();
    }
}