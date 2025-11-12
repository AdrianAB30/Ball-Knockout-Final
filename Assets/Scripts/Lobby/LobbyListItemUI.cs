using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using System; // Para Action

public class LobbyListItemUI : MonoBehaviour
{
    public static event Action<string> OnJoinLobbyRequested;

    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playersCountText;
    [SerializeField] private Button joinButton;

    private Lobby _lobby;

    public void SetLobbyData(Lobby lobby)
    {
        _lobby = lobby;
        lobbyNameText.text = lobby.Name;
        playersCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        joinButton.interactable = lobby.AvailableSlots > 0;
    }

    void OnEnable()
    {
        LobbyManager.OnJoinLobbyFailed += ReactivateButton;
    }

    void OnDisable()
    {
        LobbyManager.OnJoinLobbyFailed -= ReactivateButton;
    }

    void Start()
    {
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }
    }

    private void OnJoinButtonClicked()
    {
        if (_lobby == null) return;
        joinButton.interactable = false;
        OnJoinLobbyRequested?.Invoke(_lobby.Id); 
    }

    private void ReactivateButton()
    {
        if (gameObject.activeInHierarchy && joinButton != null)
        {
            joinButton.interactable = true;
        }
    }
}