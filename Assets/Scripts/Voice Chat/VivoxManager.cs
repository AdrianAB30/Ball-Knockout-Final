using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Core; 
using Unity.Services.Lobbies.Models;

public class VivoxManager : PersistentSingleton<VivoxManager>
{
    public static event Action<ChatMessage> OnMessageReceivedUI;
    public static event Action OnVivoxInitialized;

    private Dictionary<string, int> _savedVolumes = new Dictionary<string, int>();
    private Dictionary<string, bool> _savedMuteStates = new Dictionary<string, bool>();

    public bool IsMuted { get; private set; }
    public string CurrentVoiceChannel { get; private set; }
    public string CurrentTextChannel { get; private set; }

    public bool IsInitialized { get; private set; } = false;

    private async void Start()
    {
        await Task.Yield();

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (LobbyManager.Instance == null || PlayerAccountManager.Instance == null)
        {
            Debug.LogError("VivoxManager necesita que LobbyManager y PlayerAccountManager existan primero.");
            return;
        }

        LobbyManager.OnLobbyJoinedOrLeft += OnLobbyStateChanged;
    }
    protected virtual void OnDestroy()
    {
        LobbyManager.OnLobbyJoinedOrLeft -= OnLobbyStateChanged;

        _ = LeaveAllChannelsAsync();
    }
    private async void OnLobbyStateChanged()
    {
        Lobby currentLobby = LobbyManager.Instance.JoinedLobby;

        if (currentLobby == null)
        {
            Debug.Log("Vivox: Saliendo de todos los canales...");
            await LeaveAllChannelsAsync();
            return;
        }
        string channelName = currentLobby.Id;

        if (channelName != CurrentVoiceChannel)
        {
            Debug.Log($"Vivox: Uniéndose a los canales del lobby: {channelName}");
            await LeaveAllChannelsAsync();

            await JoinLobbyChannel(channelName);
        }
    }
    public async Task LoginVivox()
    {
        if (VivoxService.Instance.IsLoggedIn)
        {
            Debug.Log("Vivox ya está logueado.");
            return;
        }
        try
        {
            string nickName = PlayerAccountManager.Instance.PlayerName;
            LoginOptions loginOptions = new LoginOptions { DisplayName = nickName };

            VivoxService.Instance.LoggedIn += OnLoggin;
            VivoxService.Instance.LoggedOut += OnLoggOut;
            VivoxService.Instance.ChannelJoined += OnChannelJoin;
            VivoxService.Instance.ChannelMessageReceived += OnMessageRecived; 
            VivoxService.Instance.DirectedMessageReceived += OnDirectMessageRecived;

            await VivoxService.Instance.LoginAsync(loginOptions);

            Debug.Log("Te logeaste correctamente " + loginOptions.DisplayName);
        }
        catch (Exception ex)
        {
            VivoxService.Instance.LoggedIn -= OnLoggin;
            VivoxService.Instance.LoggedOut -= OnLoggOut;
            VivoxService.Instance.ChannelJoined -= OnChannelJoin;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageRecived;
            VivoxService.Instance.DirectedMessageReceived -= OnDirectMessageRecived;

            Debug.LogError("Error al loguear en Vivox:");
            Debug.LogException(ex);
        }
    }
    public async Task JoinLobbyChannel(string channelName)
    {
        Debug.Log($"<color=magenta>INTENTANDO UNIR AL CANAL ID:</color> '{channelName}'");

        if (!VivoxService.Instance.IsLoggedIn)
        {
            Debug.LogWarning("VIVOX (Lobby): No se estaba logueado. Intentando loguear ahora...");
            await LoginVivox();
        }
        if (!VivoxService.Instance.IsLoggedIn)
        {
            Debug.LogError("<color=red>VIVOX FALLÓ (LOBBY):</color> Imposible loguear. No se puede unir al canal.");
            return;
        }

        try
        {
            CurrentVoiceChannel = channelName;
            CurrentTextChannel = channelName;

            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio);

            Debug.Log($"<color=green>VIVOX ÉXITO:</color> Unido al canal de VOZ (Posicional) Y TEXTO: {channelName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red>VIVOX FALLÓ (LOBBY):</color> Error al unirse a {channelName}. {ex.Message}");
        }
    }
    public async Task LeaveTextChannel(string textChannelName = "CH1")
    {
        try
        {
            await VivoxService.Instance.LeaveChannelAsync(textChannelName);
            Debug.Log("Saliste del canal : " + textChannelName);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

    }
    public async Task SendMessageToChannel(string message , string textChannelName = "CH1")
    {
        if (!VivoxService.Instance.IsLoggedIn) return;

        try
        {
            MessageOptions messageOptions = new MessageOptions
            {
                Metadata = JsonUtility.ToJson
                ( new Dictionary<string,string>
                {
                    {"Region","Kalindor" }
                })
            };

            await VivoxService.Instance.SendChannelTextMessageAsync(textChannelName, message, messageOptions);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    public async Task SendDirectMessage(string message, string playerDisplayName)
    {
        if (!VivoxService.Instance.IsLoggedIn || string.IsNullOrEmpty(message)) return;

        try
        {
            MessageOptions messageOptions = new MessageOptions
            {
                Metadata = JsonUtility.ToJson
                (new Dictionary<string, string>
                {
                    {"Region","Kalindor" }
                })
            };

            await VivoxService.Instance.SendDirectTextMessageAsync(playerDisplayName, message, messageOptions);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    public async Task FetchHistory(string textChannelName = "CH1")
    {
        try
        {
            var historyMessages = await VivoxService.Instance.GetChannelTextMessageHistoryAsync(textChannelName);

            var reversedMessages = historyMessages.Reverse();

            foreach (VivoxMessage message in reversedMessages)
            {
                print(message.SenderDisplayName+"Ch: " + message.ChannelName + " T:" + message.ReceivedTime + "| " + message.MessageText);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
       
    }

    private void OnDirectMessageRecived(VivoxMessage message)
    {
        var chatMessage = new ChatMessage
        {
            SenderDisplayName = message.SenderDisplayName,
            SenderPlayerId = message.SenderPlayerId,
            ChannelName = message.ChannelName,
            MessageText = message.MessageText,
            IsDirectMessage = true,
            RecipientDisplayName = message.RecipientPlayerId,
        };

        OnMessageReceivedUI?.Invoke(chatMessage);
    }

    private void OnMessageRecived(VivoxMessage message)
    {
        Debug.Log($"<color=cyan>MSG RECIBIDO:</color> De: {message.SenderDisplayName}, Texto: {message.MessageText}, Canal: {message.ChannelName}");

        var chatMessage = new ChatMessage
        {
            SenderDisplayName = message.SenderDisplayName,
            SenderPlayerId = message.SenderPlayerId,
            ChannelName = message.ChannelName,
            MessageText = message.MessageText,
            IsDirectMessage = false,
            RecipientDisplayName = null
        };

        OnMessageReceivedUI?.Invoke(chatMessage);
    }

    private void OnChannelJoin(string channelName)
    {
        Debug.Log("Joining the channel "+ channelName);
    }

    private void OnLoggOut()
    {
        Debug.Log("Log out Successfull ... ");
    }

    private void OnLoggin()
    {
        Debug.Log("<color=yellow>VIVOX EVENTO:</color> Login Successfull.");

        IsInitialized = true;
        OnVivoxInitialized?.Invoke();
    }


    public void SetMicVolume(int volumeDb)
    {
        VivoxService.Instance.SetInputDeviceVolume(volumeDb);
    }
    public void SetOutputVolume(int volumeDb)
    {
        VivoxService.Instance.SetOutputDeviceVolume(volumeDb);
    }

    public void SetParticipantVolume(string unityPlayerId, int volumeDb)
    {
        if (_savedVolumes.ContainsKey(unityPlayerId))
        {
            _savedVolumes[unityPlayerId] = volumeDb;
        }
        else
        {
            _savedVolumes.Add(unityPlayerId, volumeDb);
        }

        if (string.IsNullOrEmpty(CurrentVoiceChannel) || !VivoxService.Instance.IsLoggedIn) return;

        try
        {
            var channel = VivoxService.Instance.ActiveChannels.FirstOrDefault(c => c.Key == CurrentVoiceChannel).Value;
            if (channel == null)
            {

                return;
            }


            string displayName = LobbyManager.Instance.GetPlayerNameById(unityPlayerId);
            if (string.IsNullOrEmpty(displayName) || displayName == "Unknown")
            {
                Debug.LogWarning($"No se pudo encontrar el DisplayName para el ID {unityPlayerId}");
                return;
            }

            var participant = channel.FirstOrDefault(p => p.DisplayName == displayName);


            if (participant != null)
            {
                participant.SetLocalVolume(Mathf.Clamp(volumeDb, -50, 50));

                Debug.Log($"<color=green>Volumen de {participant.DisplayName} seteado a {volumeDb}dB</color>");
            }
            else
            {
                // Debug.LogWarning($"No se encontró al participante con Nombre: {displayName} (ID: {unityPlayerId}) en el canal de Vivox (todavía).");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    public int GetSavedVolume(string unityPlayerId)
    {
        if (_savedVolumes.ContainsKey(unityPlayerId))
        {
            return _savedVolumes[unityPlayerId];
        }
        return -15;
    }
    public bool GetSavedMuteState(string unityPlayerId)
    {
        if (_savedMuteStates.ContainsKey(unityPlayerId))
        {
            return _savedMuteStates[unityPlayerId];
        }
        return false; 
    }

    public List<VivoxInputDevice> GetInputDevices()
    {
        return VivoxService.Instance.AvailableInputDevices.ToList();
    }

    public List<VivoxOutputDevice> GetOutputDevices()
    {
        return VivoxService.Instance.AvailableOutputDevices.ToList();
    }

    public void SetInputDevice(VivoxInputDevice device)
    {
        VivoxService.Instance.SetActiveInputDeviceAsync(device);
        Debug.Log($"<color=cyan>VIVOX:</color> Dispositivo de Entrada seteado a: {device.DeviceName}");
    }

    public void SetOutputDevice(VivoxOutputDevice device)
    {
        VivoxService.Instance.SetActiveOutputDeviceAsync(device);
        Debug.Log($"<color=cyan>VIVOX:</color> Dispositivo de Salida seteado a: {device.DeviceName}");
    }
    public void ToggleMute()
    {
        if (!VivoxService.Instance.IsLoggedIn) return;

        if (VivoxService.Instance.IsInputDeviceMuted)
        {
            VivoxService.Instance.UnmuteInputDevice();
        }
        else
        {
            VivoxService.Instance.MuteInputDevice();
        }

        IsMuted = VivoxService.Instance.IsInputDeviceMuted;

        Debug.Log(IsMuted ? "Micrófono MUTEADO" : "Micrófono ACTIVADO");
    }

    public async Task LeaveAllChannelsAsync()
    {
        if (!VivoxService.Instance.IsLoggedIn) return;

        try
        {
            if (!string.IsNullOrEmpty(CurrentTextChannel))
            {
                await VivoxService.Instance.LeaveChannelAsync(CurrentTextChannel);
            }
            if (!string.IsNullOrEmpty(CurrentVoiceChannel))
            {
                await VivoxService.Instance.LeaveChannelAsync(CurrentVoiceChannel);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            CurrentTextChannel = null;
            CurrentVoiceChannel = null;
            Debug.Log("Has salido de todos los canales de Vivox.");
        }
    }
    public bool IsPlayerSpeaking(string unityPlayerId)
    {
        if (!VivoxService.Instance.IsLoggedIn || string.IsNullOrEmpty(CurrentVoiceChannel))
            return false;

        if (VivoxService.Instance.ActiveChannels.TryGetValue(CurrentVoiceChannel, out var channelSession))
        {
            string displayName = LobbyManager.Instance.GetPlayerNameById(unityPlayerId);
            if (string.IsNullOrEmpty(displayName)) return false;

            var participant = channelSession.FirstOrDefault(p => p.DisplayName == displayName);

            if (participant != null)
            {
                return participant.AudioEnergy > 0.1f;
            }
        }
        return false;
    }
    public bool ToggleParticipantMute(string unityPlayerId)
    {
        if (string.IsNullOrEmpty(CurrentVoiceChannel) || !VivoxService.Instance.IsLoggedIn) return false;

        try
        {
            var channel = VivoxService.Instance.ActiveChannels.FirstOrDefault(c => c.Key == CurrentVoiceChannel).Value;
            if (channel == null) return false;

            string displayName = LobbyManager.Instance.GetPlayerNameById(unityPlayerId);
            if (string.IsNullOrEmpty(displayName)) return false;

            var participant = channel.FirstOrDefault(p => p.DisplayName == displayName);

            if (participant != null)
            {
                bool isCurrentlyMuted = participant.IsMuted;

                if (isCurrentlyMuted) participant.UnmutePlayerLocally();
                else participant.MutePlayerLocally();

                bool newMuteState = !isCurrentlyMuted;

                if (_savedMuteStates.ContainsKey(unityPlayerId))
                {
                    _savedMuteStates[unityPlayerId] = newMuteState;
                }
                else
                {
                    _savedMuteStates.Add(unityPlayerId, newMuteState);
                }

                Debug.Log($"Participante {displayName} ahora está {(newMuteState ? "MUTEADO" : "DESMUTEADO")}");
                return newMuteState;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        return false;
    }
}