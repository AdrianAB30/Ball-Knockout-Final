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

    public bool IsMuted { get; private set; }
    public string CurrentVoiceChannel { get; private set; }
    public string CurrentTextChannel { get; private set; }

    private async void Start()
    {
        await Task.Yield();
        if (LobbyManager.Instance == null || PlayerAccountManager.Instance == null)
        {
            Debug.LogError("VivoxManager necesita que LobbyManager y PlayerAccountManager existan primero.");
            return;
        }

        LobbyManager.OnLobbyJoinedOrLeft += OnLobbyStateChanged;

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        await LoginVivox();

        ShowInputDevices();
        ShowOutputDevices();
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

            await JoinVoiceChannel(channelName);
            await JoinTextChannel(channelName);
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

            await VivoxService.Instance.LoginAsync(loginOptions);

            VivoxService.Instance.LoggedIn += OnLoggin;
            VivoxService.Instance.LoggedOut += OnLoggOut;
            VivoxService.Instance.ChannelJoined += OnChannelJoin;
            VivoxService.Instance.ChannelMessageReceived += OnMessageRecived;
            VivoxService.Instance.DirectedMessageReceived += OnDirectMessageRecived;

            Debug.Log("Te logeaste correctamente " + loginOptions.DisplayName);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al loguear en Vivox:");
            Debug.LogException(ex);
        }
    }
    public async Task JoinTextChannel(string textChannelName = "CH1")
    {
        if (!VivoxService.Instance.IsLoggedIn) return;
        try
        {
            CurrentTextChannel = textChannelName;
            await VivoxService.Instance.JoinGroupChannelAsync(textChannelName, ChatCapability.TextOnly);
            Debug.Log("Te uniste al canal de TEXTO: " + textChannelName);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    
    public async Task JoinVoiceChannel(string textChannelName = "CH1")
    {
        if (!VivoxService.Instance.IsLoggedIn) return;
        try
        {
            CurrentVoiceChannel = textChannelName;
            Channel3DProperties properties = new Channel3DProperties();
            await VivoxService.Instance.JoinPositionalChannelAsync(textChannelName, ChatCapability.AudioOnly, properties);
            Debug.Log("Te uniste al canal de VOZ: " + textChannelName);
            await VivoxService.Instance.JoinGroupChannelAsync(textChannelName, ChatCapability.AudioOnly);
            await VivoxService.Instance.JoinEchoChannelAsync(textChannelName, ChatCapability.AudioOnly);

            Debug.Log("Te uniste al canal : " + textChannelName);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    #region TextStuff
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
        Debug.Log("Login Successfull ... ");
    }
    #endregion


    public void SetMicVolume(int volumeDb)
    {
        VivoxService.Instance.SetInputDeviceVolume(volumeDb);
    }
    public void SetOutputVolume(int volumeDb)
    {
        VivoxService.Instance.SetOutputDeviceVolume(volumeDb);
    }

    public void SetParticipantVolume(string channelID, string username , int volume = 15)
    {
        VivoxParticipant participant = 
            VivoxService.Instance.ActiveChannels.FirstOrDefault(x => x.Key.Equals(channelID))
            .Value.FirstOrDefault(x => x.PlayerId.Equals(username));


        participant.SetLocalVolume(volume);
    }

    public async void SelectInputDevice(string deviceId)
    {
        List<VivoxInputDevice> inputs =  VivoxService.Instance.AvailableInputDevices.ToList();

        VivoxInputDevice input = inputs.Find(x => x.DeviceID.Equals(deviceId));

        await VivoxService.Instance.SetActiveInputDeviceAsync(input);
    }
    public async void SelectOutputDevice(string deviceId)
    {
        List<VivoxOutputDevice> inputs = VivoxService.Instance.AvailableOutputDevices.ToList();

        VivoxOutputDevice input = inputs.Find(x => x.DeviceID.Equals(deviceId));

        await VivoxService.Instance.SetActiveOutputDeviceAsync(input);
    }
    public void ShowInputDevices()
    {
        foreach (var device in VivoxService.Instance.AvailableInputDevices)
        {
            Debug.Log("Input: " + device.DeviceName + "ID: " + device.DeviceID);
        }
    
    }
    public void ShowOutputDevices()
    {
        foreach (var device in VivoxService.Instance.AvailableOutputDevices)
        {
            Debug.Log("Input: " + device.DeviceName + "ID: " + device.DeviceID);
        }

    }

    public void ToggleMute()
    {
        if (!VivoxService.Instance.IsLoggedIn) return;
        IsMuted = !IsMuted;
        VivoxService.Instance.MuteOutputDevice();
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

    public void SetParticipantVolume(string unityPlayerId, int volumeDb)
    {
        if (string.IsNullOrEmpty(CurrentVoiceChannel) || !VivoxService.Instance.IsLoggedIn) return;

        try
        {
            var channel = VivoxService.Instance.ActiveChannels.FirstOrDefault(c => c.Key == CurrentVoiceChannel).Value;
            if (channel == null)
            {
                Debug.LogWarning($"No se encontró el canal de voz {CurrentVoiceChannel}");
                return;
            }

            var participant = channel.FirstOrDefault(p => p.PlayerId == unityPlayerId);
            if (participant != null)
            {
                participant.SetLocalVolume(Mathf.Clamp(volumeDb, -50, 50));
                Debug.Log($"Volumen de {participant.DisplayName} seteado a {volumeDb}dB");
            }
            else
            {
                Debug.LogWarning($"No se encontró al participante con ID {unityPlayerId} en el canal.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}