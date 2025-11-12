using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Collections;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform chatContentContainer; // El Content del Scroll View
    [SerializeField] private GameObject chatMessagePrefab; // Prefab para cada mensaje
    [SerializeField] private ScrollRect chatScrollRect; // Para el auto-scroll

    private List<GameObject> _spawnedChatMessages = new List<GameObject>();
    private string _currentPlayerId;

    void Awake()
    {
        _currentPlayerId = AuthenticationService.Instance.PlayerId;
    }

    void OnEnable()
    {
        ChatManager.OnNewMessage += DisplayNewMessage;
        LobbyManager.OnLobbyJoinedOrLeft += OnLobbyStateChanged;
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }
        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(OnInputSubmit); 
        }
    }

    void OnDisable()
    {
        ChatManager.OnNewMessage -= DisplayNewMessage;
        LobbyManager.OnLobbyJoinedOrLeft -= OnLobbyStateChanged;
        ClearChatMessages();
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
        }
        if (chatInputField != null)
        {
            chatInputField.onSubmit.RemoveListener(OnInputSubmit);
        }
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
        await ChatManager.Instance.ProcessAndSendMessage(message);
    }

    private void DisplayNewMessage(ChatMessage message)
    {
        string formattedMessage = message.GetFormattedMessage(_currentPlayerId);
        if (string.IsNullOrEmpty(formattedMessage))
        {
            return; // No muestres este mensaje (es privado para otra persona)
        }

        GameObject messageGO = Instantiate(chatMessagePrefab, chatContentContainer);
        TextMeshProUGUI messageText = messageGO.GetComponent<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = formattedMessage;
        }
        _spawnedChatMessages.Add(messageGO);

        if (_spawnedChatMessages.Count > 50) // Límite de mensajes
        {
            Destroy(_spawnedChatMessages[0]);
            _spawnedChatMessages.RemoveAt(0);
        }

        // Forzar el scroll hacia abajo
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