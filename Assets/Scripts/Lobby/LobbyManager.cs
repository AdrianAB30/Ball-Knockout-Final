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
    public const string KEY_CHAT_MESSAGE = "ChatMessage";

    // --- Caché de Mensajes ---
    private Dictionary<string, string> _lastPlayerChatMessages = new Dictionary<string, string>();

    // --- Propiedades Públicas ---
    public Lobby HostLobby { get; private set; }
    public Lobby JoinedLobby { get; private set; }
    public string CurrentRelayIP { get; private set; }
    public string CurrentRelayCode { get; private set; }

    // --- Eventos ---
    public static event Action<List<Lobby>> OnLobbyListChanged;
    public static event Action OnLobbyJoinedOrLeft;
    public static event Action<Lobby> OnLobbyUpdated;
    public static event Action<ChatMessage> OnChatMessageReceived;

    // --- Eventos de Fallo ---
    public static event Action OnCreateLobbyFailed;
    public static event Action OnJoinLobbyFailed;
    public static event Action OnQuickJoinFailed;
    public static event Action OnJoinByCodeFailed;
    public static event Action OnDeleteLobbyFailed;
    public static event Action OnReadyToggleFailed;
    public static event Action OnKickPlayerFailed;

    private float _heartbeatTimer;
    [SerializeField] public RelayServiceManager _relayManager;
    private ILobbyEvents _lobbyEvents;

    // --- Suscripción a Eventos de la UI ---
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
        // Desuscribirse de todos los eventos
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

    // --- Métodos de Eventos de Lobby ---
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

    // --- ¡MÉTODO OnLobbyChanged CORREGIDO! ---
    // Esta es la versión que compila Y arregla el bug de sincronización.
    private void OnLobbyChanged(ILobbyChanges lobbyChange)
    {
        if (JoinedLobby == null) return;

        // 1. APLICA los cambios al lobby PRIMERO.
        lobbyChange.ApplyToLobby(JoinedLobby);

        // 2. AHORA, revisa los datos de chat actualizados en 'JoinedLobby'
        foreach (var player in JoinedLobby.Players)
        {
            if (player.Data != null && player.Data.TryGetValue(KEY_CHAT_MESSAGE, out PlayerDataObject chatData))
            {
                string messageJson = chatData.Value;
                _lastPlayerChatMessages.TryGetValue(player.Id, out string lastMessageJson);

                if (!string.IsNullOrEmpty(messageJson) && messageJson != lastMessageJson)
                {
                    _lastPlayerChatMessages[player.Id] = messageJson; // Actualiza el caché
                    try
                    {
                        ChatMessage receivedMessage = JsonUtility.FromJson<ChatMessage>(messageJson);
                        OnChatMessageReceived?.Invoke(receivedMessage);
                    }
                    catch (Exception e) { Debug.LogError($"Error deserializando chat: {e}"); }
                }
            }
        }

        Debug.Log("Lobby actualizado vía Evento.");
        OnLobbyUpdated?.Invoke(JoinedLobby); // Refresca la UI
    }

    private void OnKickedFromLobby()
    {
        Debug.LogWarning("¡Has sido kickeado del lobby por el Host!");
        ClearLobbyData();
        OnLobbyJoinedOrLeft?.Invoke();
    }

    // --- Lógica de Datos ---

    private Player GetNewPlayerData()
    {
        var playerData = new Dictionary<string, PlayerDataObject>
        {
            { KEY_PLAYER_NAME, new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Member,
                value: PlayerAccountManager.Instance.PlayerName) },
            { KEY_PLAYER_READY, new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Member,
                value: "false") }
            // No se añade KEY_CHAT_MESSAGE aquí
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
        _lastPlayerChatMessages.Clear(); // <-- ¡Limpia el caché de chat!
    }

    // --- Métodos Privados (Llamados por Eventos de UI) ---

    private async void CreateLobby(string lobbyName, int maxPlayers)
    {
        try
        {
            string relayCode = await _relayManager.CreateRelay(maxPlayers);
            if (string.IsNullOrEmpty(relayCode)) throw new Exception("Failed to create Relay.");

            CurrentRelayIP = _relayManager.RelayIpV4;
            CurrentRelayCode = _relayManager.RelayJoinCode;
            _lastPlayerChatMessages.Clear(); // Limpia el caché al crear

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetNewPlayerData(),
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
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
            _lastPlayerChatMessages.Clear(); // Limpia el caché al unirse

            await SubscribeToLobbyEvents(JoinedLobby);
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
            _lastPlayerChatMessages.Clear();

            await SubscribeToLobbyEvents(JoinedLobby);
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
            _lastPlayerChatMessages.Clear();

            await SubscribeToLobbyEvents(JoinedLobby);
            OnLobbyJoinedOrLeft?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Join by code failed: {e}");
            OnJoinByCodeFailed?.Invoke();
        }
    }

    // --- ¡MÉTODO DE ACTUALIZACIÓN UNIFICADO! (SOLUCIÓN AL BUG DE SINCRONIZACIÓN) ---

    /// <summary>
    /// Actualiza los datos de un jugador (Ready o Chat) enviando SIEMPRE
    /// el estado completo para evitar que los datos se sobrescriban.
    /// </summary>
    private async Task UpdatePlayerDataAsync(ChatMessage chatMessage = null, bool? isReady = null)
    {
        if (JoinedLobby == null) return;

        string playerId = AuthenticationService.Instance.PlayerId;
        var currentPlayer = JoinedLobby.Players.Find(p => p.Id == playerId);
        if (currentPlayer == null) return; // No estamos en el lobby

        try
        {
            var playerData = new Dictionary<string, PlayerDataObject>();

            // 1. Añadir Nombre (siempre debe estar)
            // (Asegurarse de que el nombre exista en los datos)
            if (currentPlayer.Data.TryGetValue(KEY_PLAYER_NAME, out var nameData))
            {
                playerData[KEY_PLAYER_NAME] = nameData;
            }
            else
            {
                // Si no existe, tómalo del PlayerAccountManager (debería existir desde GetNewPlayerData)
                playerData[KEY_PLAYER_NAME] = new PlayerDataObject(
                    PlayerDataObject.VisibilityOptions.Member,
                    PlayerAccountManager.Instance.PlayerName);
            }


            // 2. Determinar y añadir estado 'Ready'
            bool currentReadyState = false;
            if (currentPlayer.Data.TryGetValue(KEY_PLAYER_READY, out var readyData))
            {
                bool.TryParse(readyData.Value, out currentReadyState);
            }
            playerData[KEY_PLAYER_READY] = new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Member,
                value: (isReady ?? currentReadyState).ToString()
            );

            // 3. Determinar y añadir mensaje de Chat
            string messageJson = "";
            if (chatMessage != null)
            {
                // Estamos enviando un mensaje nuevo
                messageJson = JsonUtility.ToJson(chatMessage);
            }
            else
            {
                // No estamos enviando un chat, así que reenviamos el último mensaje
                _lastPlayerChatMessages.TryGetValue(playerId, out messageJson);
            }
            playerData[KEY_CHAT_MESSAGE] = new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Member,
                value: messageJson ?? "" // Asegurarse de no enviar nulo
            );

            // 4. Enviar la actualización COMPLETA
            await LobbyService.Instance.UpdatePlayerAsync(
                JoinedLobby.Id,
                playerId,
                new UpdatePlayerOptions { Data = playerData }
            );

            // 5. Si enviamos un chat, actualiza el caché local inmediatamente
            if (chatMessage != null)
            {
                _lastPlayerChatMessages[playerId] = messageJson;
                // No disparamos OnChatMessageReceived aquí, OnLobbyChanged lo hará
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update player data: {e}");
            if (isReady != null) OnReadyToggleFailed?.Invoke();
            // (Podríamos añadir un OnChatSendFailed?.Invoke() aquí también)
        }
    }

    // --- Métodos que llaman al actualizador unificado ---

    private async void UpdatePlayerReady(bool isReady)
    {
        await UpdatePlayerDataAsync(null, isReady);
    }

    // ¡Este método DEBE ser público para que ChatManager lo llame!
    public async Task SendChatMessage(ChatMessage message)
    {
        await UpdatePlayerDataAsync(message, null);
    }

    // ---

    private async void LeaveLobby()
    {
        if (JoinedLobby == null) return;
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
            await LobbyService.Instance.UpdateLobbyAsync(HostLobby.Id, new UpdateLobbyOptions { IsLocked = true });
            Debug.Log("Host is starting the game!");
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene",
                UnityEngine.SceneManagement.LoadSceneMode.Single);
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
        if (JoinedLobby == null) return; // No estamos en un lobby, no hay nada que actualizar

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

            // ¡No necesitas llamar a OnLobbyUpdated aquí!
            // El servidor disparará el evento OnLobbyChanged para todos 
            // automáticamente, lo que refrescará la UI.
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Error al actualizar el nombre del jugador en el lobby: {e}");
        }
    }
}