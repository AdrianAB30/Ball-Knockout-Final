using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using System;

public class LobbyManager : PersistentSingleton<LobbyManager>
{
    // --- Constantes de Datos ---
    private const string KEY_RELAY_CODE = "RelayJoinCode";
    public const string KEY_PLAYER_READY = "PlayerReady";
    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_MAP_TYPE = "MapType";
    public const string KEY_AVATAR_ID = "AvatarID";


    // --- Propiedades P�blicas ---
    public Lobby HostLobby { get; private set; }
    public Lobby JoinedLobby { get; private set; }
    public string CurrentRelayIP { get; private set; }
    public string CurrentRelayCode { get; private set; }

    // --- Eventos ---
    public static event Action<List<Lobby>> OnLobbyListChanged;
    public static event Action OnLobbyJoinedOrLeft;
    public static event Action<Lobby> OnLobbyUpdated;

    // --- Eventos de Fallo ---
    public static event Action OnCreateLobbyFailed;
    public static event Action OnJoinLobbyFailed;
    public static event Action OnQuickJoinFailed;
    public static event Action OnJoinByCodeFailed;
    public static event Action OnDeleteLobbyFailed;
    public static event Action OnReadyToggleFailed;
    public static event Action OnKickPlayerFailed;

    [SerializeField] public RelayServiceManager _relayManager;
    [SerializeField] private GameConfigurationSO gameConfig;

    private ILobbyEvents _lobbyEvents;
    private float _heartbeatTimer;

    protected override void Awake()
    {
        base.Awake();

        CreateLobbyUI.OnCreateLobbyRequested += CreateLobby;
        LobbyListUI.OnRefreshRequested += RefreshLobbyList;
        LobbyListUI.OnJoinByCodeRequested += JoinLobbyByCode;
        LobbyListUI.OnQuickJoinRequested += QuickJoinLobby;
        LobbyListItemUI.OnJoinLobbyRequested += JoinLobby;
        CurrentLobbyUI.OnLeaveLobbyRequested += LeaveLobby;
        CurrentLobbyUI.OnDeleteLobbyRequested += DeleteLobby;
        CurrentLobbyUI.OnStartGameRequested += StartGame;
        CurrentLobbyUI.OnReadyToggled += UpdatePlayerReady;
        PlayerListItemUI.OnKickPlayerRequested += KickPlayer;
    }

    private void OnDestroy()
    {
        CreateLobbyUI.OnCreateLobbyRequested -= CreateLobby;
        LobbyListUI.OnRefreshRequested -= RefreshLobbyList;
        LobbyListUI.OnJoinByCodeRequested -= JoinLobbyByCode;
        LobbyListUI.OnQuickJoinRequested -= QuickJoinLobby;
        LobbyListItemUI.OnJoinLobbyRequested -= JoinLobby;
        CurrentLobbyUI.OnLeaveLobbyRequested -= LeaveLobby;
        CurrentLobbyUI.OnDeleteLobbyRequested -= DeleteLobby;
        CurrentLobbyUI.OnStartGameRequested -= StartGame;
        CurrentLobbyUI.OnReadyToggled -= UpdatePlayerReady;
        PlayerListItemUI.OnKickPlayerRequested -= KickPlayer;

        _lobbyEvents?.UnsubscribeAsync();
    }

    async void Start()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    void Update()
    {
        HandleLobbyHeartbeat();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (HostLobby == null) return;
        _heartbeatTimer -= Time.deltaTime;
        if (_heartbeatTimer <= 0)
        {
            _heartbeatTimer = 15f;
            try { await LobbyService.Instance.SendHeartbeatPingAsync(HostLobby.Id); }
            catch (LobbyServiceException e) { Debug.LogError($"Failed to send lobby heartbeat: {e}"); }
        }
    }

    private async Task SubscribeToLobbyEvents(Lobby lobby)
    {
        try
        {
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.KickedFromLobby += OnKickedFromLobby;
            _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);
        }
        catch (Exception e) { Debug.LogError($"Error subscribing to lobby events: {e}"); }
    }

    private void OnLobbyChanged(ILobbyChanges lobbyChange)
    {
        if (JoinedLobby == null) return;
        lobbyChange.ApplyToLobby(JoinedLobby);
        Debug.Log("Lobby actualizado vía Evento.");
        OnLobbyUpdated?.Invoke(JoinedLobby); 
    }

    private void OnKickedFromLobby()
    {
        Debug.LogWarning("�Has sido kickeado del lobby por el Host!");
        ClearLobbyData();
        OnLobbyJoinedOrLeft?.Invoke();
    }


    private Player GetNewPlayerData()
    {
        var playerData = new Dictionary<string, PlayerDataObject>
        {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerAccountManager.Instance.PlayerName) },
            { KEY_PLAYER_READY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false") },
            { KEY_AVATAR_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerAccountManager.Instance.CurrentProfile.avatarIndex.ToString()) }
        };
        return new Player { Data = playerData };
    }

    private void ClearLobbyData()
    {
        _lobbyEvents?.UnsubscribeAsync();
        JoinedLobby = null;
        HostLobby = null;
        CurrentRelayIP = null;
        CurrentRelayCode = null;
    }


    private async void CreateLobby(string lobbyName, int maxPlayers)
    {
        try
        {
            int myAvatarIndex = PlayerAccountManager.Instance.CurrentProfile.avatarIndex;

            string relayCode = await _relayManager.CreateRelay(maxPlayers);
            if (string.IsNullOrEmpty(relayCode)) throw new Exception("Failed to create Relay.");

            CurrentRelayIP = _relayManager.RelayIpV4;
            CurrentRelayCode = _relayManager.RelayJoinCode;

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerAccountManager.Instance.PlayerName) },
                        { KEY_PLAYER_READY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false") },
                        { KEY_AVATAR_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, myAvatarIndex.ToString()) }
                    }
                },
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) },
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, "1v1 Competitivo") },
                    { KEY_MAP_TYPE, new DataObject(DataObject.VisibilityOptions.Public, "Estadio Pixel") }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            HostLobby = lobby;
            JoinedLobby = lobby;
            _heartbeatTimer = 15f;

            await SubscribeToLobbyEvents(lobby);

            OnLobbyJoinedOrLeft?.Invoke();

            OnLobbyUpdated?.Invoke(JoinedLobby);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
            OnCreateLobbyFailed?.Invoke();
        }
    }

    private async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GE)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(true, QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
            OnLobbyListChanged?.Invoke(queryResponse.Results);
        }
        catch (LobbyServiceException e) { Debug.LogError($"Failed to query lobbies: {e}"); }
    }

    private async void JoinLobby(string lobbyId)
    {
        try
        {
            var joinOptions = new JoinLobbyByIdOptions { Player = GetNewPlayerData() };
            JoinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);

            string relayCode = JoinedLobby.Data[KEY_RELAY_CODE].Value;
            await _relayManager.JoinRelay(relayCode);

            CurrentRelayIP = _relayManager.RelayIpV4;
            CurrentRelayCode = _relayManager.RelayJoinCode;

            await SubscribeToLobbyEvents(JoinedLobby);
            Debug.Log("Unido al Lobby. Logueando en Vivox y Uniendo a canales.");


            OnLobbyJoinedOrLeft?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e}");
            OnJoinLobbyFailed?.Invoke();
        }
    }

    private async void QuickJoinLobby()
    {
        try
        {
            var quickJoinOptions = new QuickJoinLobbyOptions { Player = GetNewPlayerData() };
            JoinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinOptions);

            string relayCode = JoinedLobby.Data[KEY_RELAY_CODE].Value;
            await _relayManager.JoinRelay(relayCode);

            CurrentRelayIP = _relayManager.RelayIpV4;
            CurrentRelayCode = _relayManager.RelayJoinCode;

            await SubscribeToLobbyEvents(JoinedLobby);

            Debug.Log("Unido al Lobby. Logueando en Vivox y Uniendo a canales.");


            OnLobbyJoinedOrLeft?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Quick Join failed: {e}");
            OnQuickJoinFailed?.Invoke();
        }
    }

    private async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            var joinOptions = new JoinLobbyByCodeOptions { Player = GetNewPlayerData() };
            JoinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);

            string relayCode = JoinedLobby.Data[KEY_RELAY_CODE].Value;
            await _relayManager.JoinRelay(relayCode);

            CurrentRelayIP = _relayManager.RelayIpV4;
            CurrentRelayCode = _relayManager.RelayJoinCode;

            await SubscribeToLobbyEvents(JoinedLobby);

            Debug.Log("Unido al Lobby. Logueando en Vivox y Uniendo a canales.");
            OnLobbyJoinedOrLeft?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Join by code failed: {e}");
            OnJoinByCodeFailed?.Invoke();
        }
    }

    private async void UpdatePlayerReady(bool isReady)
    {
        if (JoinedLobby == null) return;
        try
        {
            Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>
        {
            { KEY_PLAYER_READY, new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Member,
                value: isReady.ToString()) }
        };
            string playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.UpdatePlayerAsync(
                JoinedLobby.Id,
                playerId,
                new UpdatePlayerOptions { Data = playerData }
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update player ready state: {e}");
            OnReadyToggleFailed?.Invoke();
        }
    }

    private async void LeaveLobby()
    {
        if (JoinedLobby == null) return;
        if (VivoxManager.Instance != null)
    {
        await VivoxManager.Instance.LeaveAllChannelsAsync();
    }
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e) { Debug.LogError($"Failed to leave lobby: {e}"); }

        ClearLobbyData();
        OnLobbyJoinedOrLeft?.Invoke();
    }

    private async void DeleteLobby()
    {
        if (HostLobby == null) return;
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(HostLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to delete lobby: {e}");
            OnDeleteLobbyFailed?.Invoke();
        }

        ClearLobbyData();
        OnLobbyJoinedOrLeft?.Invoke();
    }

    private async void KickPlayer(string playerId)
    {
        if (HostLobby == null) return;
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(HostLobby.Id, playerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to kick player: {e}");
            OnKickPlayerFailed?.Invoke();
        }
    }

    private async void StartGame()
    {
        if (HostLobby == null) return;
        try
        {
            if (gameConfig != null) gameConfig.SetOnlineMode();

            await LobbyService.Instance.UpdateLobbyAsync(HostLobby.Id, new UpdateLobbyOptions { IsLocked = true });
            Debug.Log("Host is starting the game!");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("NetworkManager no está listo para cambiar de escena.");
            }
        }
        catch (System.Exception e) { Debug.LogError($"Failed to start game: {e}"); }
    }

    public string GetPlayerNameById(string playerId)
    {
        if (JoinedLobby == null || string.IsNullOrEmpty(playerId)) return "Unknown";
        Player player = JoinedLobby.Players.Find(p => p.Id == playerId);
        if (player != null && player.Data != null && player.Data.TryGetValue(KEY_PLAYER_NAME, out PlayerDataObject nameData))
        {
            return nameData.Value;
        }
        return "Unknown";
    }
    public async Task UpdatePlayerNameInLobby(string newName)
    {
        if (JoinedLobby == null) return; 

        try
        {
            Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>
            {
                { KEY_PLAYER_NAME, new PlayerDataObject(
                    visibility: PlayerDataObject.VisibilityOptions.Member,
                    value: newName) }
            };

            string playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.UpdatePlayerAsync(
                JoinedLobby.Id,
                playerId,
                new UpdatePlayerOptions { Data = playerData }
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Error al actualizar el nombre del jugador en el lobby: {e}");
        }
    }
    public string GetPlayerIdByName(string playerName)
    {
        if (JoinedLobby == null) return null;

        foreach (var player in JoinedLobby.Players)
        {
            if (player.Data != null && player.Data.TryGetValue(KEY_PLAYER_NAME, out PlayerDataObject nameData))
            {
                if (nameData.Value == playerName)
                {
                    return player.Id; 
                }
            }
        }
        return null;
    }
}