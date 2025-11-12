using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using Unity.Services.Authentication;
using System;

public class CurrentLobbyUI : MonoBehaviour
{
    public static event Action OnLeaveLobbyRequested;
    public static event Action OnDeleteLobbyRequested;
    public static event Action OnStartGameRequested;
    public static event Action<bool> OnReadyToggled;
    public static event Action OnChooseMapRequested;


    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;

    [Header("Botones")]
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button deleteLobbyButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private Button chooseMapButton;

    private List<GameObject> _spawnedPlayerItems = new List<GameObject>();
    private bool _isPlayerReady = false;

    void OnEnable()
    {
        LobbyManager.OnLobbyJoinedOrLeft += OnLobbyStateChanged;
        LobbyManager.OnLobbyUpdated += UpdateLobbyUI;

        // --- ¡CORRECCIÓN! Escucha los eventos del LobbyManager ---
        LobbyManager.OnDeleteLobbyFailed += ReactivateDeleteButton;
        LobbyManager.OnReadyToggleFailed += ReactivateReadyButton;

        OnLobbyStateChanged();
    }

    void OnDisable()
    {
        LobbyManager.OnLobbyJoinedOrLeft -= OnLobbyStateChanged;
        LobbyManager.OnLobbyUpdated -= UpdateLobbyUI;

        // --- ¡CORRECCIÓN! Se desuscribe de los eventos del LobbyManager ---
        LobbyManager.OnDeleteLobbyFailed -= ReactivateDeleteButton;
        LobbyManager.OnReadyToggleFailed -= ReactivateReadyButton;
    }

    void Start()
    {
        if (leaveLobbyButton != null) leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyButtonClicked);
        if (chooseMapButton != null) chooseMapButton.onClick.AddListener(OnChooseMapClicked);
        if (deleteLobbyButton != null) deleteLobbyButton.onClick.AddListener(OnDeleteLobbyClicked);
    }

    void OnDestroy()
    {
        // Limpia los listeners de los botones
        if (leaveLobbyButton != null) leaveLobbyButton.onClick.RemoveListener(OnLeaveLobbyClicked);
        if (startGameButton != null) startGameButton.onClick.RemoveListener(OnStartGameClicked);
        if (readyButton != null) readyButton.onClick.RemoveListener(OnReadyButtonClicked);
        if (chooseMapButton != null) chooseMapButton.onClick.RemoveListener(OnChooseMapClicked);
        if (deleteLobbyButton != null) deleteLobbyButton.onClick.RemoveListener(OnDeleteLobbyClicked);
    }

    private void OnLobbyStateChanged()
    {
        bool inLobby = LobbyManager.Instance.JoinedLobby != null;
        gameObject.SetActive(inLobby);

        if (inLobby)
        {
            UpdateLobbyUI(LobbyManager.Instance.JoinedLobby);
            _isPlayerReady = false;
            UpdateReadyButtonUI();
        }
        else
        {
            ClearPlayerList();
        }
    }

    private void UpdateLobbyUI(Lobby lobby)
    {
        if (lobby == null) return;

        lobbyNameText.text = lobby.Name;
        lobbyCodeText.text = $"Lobby Code: {lobby.LobbyCode}";

        ClearPlayerList();
        bool isHost = LobbyManager.Instance.JoinedLobby.HostId == AuthenticationService.Instance.PlayerId;

        foreach (Player player in lobby.Players)
        {
            GameObject itemGO = Instantiate(playerListItemPrefab, playerListContainer);
            PlayerListItemUI itemUI = itemGO.GetComponent<PlayerListItemUI>();
            if (itemUI != null)
            {
                itemUI.SetPlayerData(player, isHost);
                _spawnedPlayerItems.Add(itemGO);
            }
        }

        startGameButton.gameObject.SetActive(isHost);
        chooseMapButton.gameObject.SetActive(isHost);
        deleteLobbyButton.gameObject.SetActive(isHost);
        readyButton.gameObject.SetActive(true);

        if (isHost)
        {
            bool allPlayersReady = true;
            foreach (Player player in lobby.Players)
            {

                if (player.Data == null ||
          !player.Data.TryGetValue(LobbyManager.KEY_PLAYER_READY, out PlayerDataObject readyData) ||
          !bool.Parse(readyData.Value))
                {
                    allPlayersReady = false;
                    break;
                }
            }
            startGameButton.interactable = allPlayersReady;
        }

        Player currentPlayer = lobby.Players.Find(p => p.Id == AuthenticationService.Instance.PlayerId);
        if (currentPlayer != null && currentPlayer.Data != null &&
            currentPlayer.Data.TryGetValue(LobbyManager.KEY_PLAYER_READY, out PlayerDataObject currentReadyData))
        {
            _isPlayerReady = bool.Parse(currentReadyData.Value);
        }
        UpdateReadyButtonUI();
    }

    private void ClearPlayerList()
    {
        foreach (GameObject item in _spawnedPlayerItems) Destroy(item);
        _spawnedPlayerItems.Clear();
    }

    private void OnLeaveLobbyClicked()
    {
        OnLeaveLobbyRequested?.Invoke();
    }

    private void OnDeleteLobbyClicked()
    {
        if (deleteLobbyButton != null) deleteLobbyButton.interactable = false;
        OnDeleteLobbyRequested?.Invoke();
    }

    private void OnStartGameClicked()
    {
        OnStartGameRequested?.Invoke();
    }

    private void OnChooseMapClicked()
    {
        OnChooseMapRequested?.Invoke();
    }

    private void OnReadyButtonClicked()
    {
        _isPlayerReady = !_isPlayerReady;
        readyButton.interactable = false;
        OnReadyToggled?.Invoke(_isPlayerReady);
        UpdateReadyButtonUI();
    }

    private void UpdateReadyButtonUI()
    {
        if (readyButtonText != null)
        {
            readyButtonText.text = _isPlayerReady ? "Unready" : "Ready";
        }
        if (!readyButton.interactable)
        {
            readyButton.interactable = true;
        }
    }

    private void ReactivateDeleteButton()
    {
        if (deleteLobbyButton != null) deleteLobbyButton.interactable = true;
    }

    private void ReactivateReadyButton()
    {
        if (readyButton != null) readyButton.interactable = true;
    }
}