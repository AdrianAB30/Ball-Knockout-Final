using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;

public class VivoxManager : PersistentSingleton<VivoxManager>
{
    
    void Start()
    {
        
    }

    public async Task LoginVivox()
    {
        try
        {
            string nickName = await AuthenticationService.Instance.GetPlayerNameAsync();

            LoginOptions loginOptions = new LoginOptions();

            loginOptions.DisplayName = nickName;

            await VivoxService.Instance.LoginAsync(loginOptions);
            //->Eventos
            VivoxService.Instance.LoggedIn += OnLoggin;
            VivoxService.Instance.LoggedOut += OnLoggOut;

            VivoxService.Instance.ChannelJoined += OnChannelJoin;

            VivoxService.Instance.ChannelMessageReceived += OnMessageRecived;
            VivoxService.Instance.DirectedMessageReceived += OnDirectMessageRecived;

            Debug.Log("Te logeaste correctamente " + loginOptions.DisplayName);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }


    }
    public async Task JoinTextChannel(string textChannelName = "CH1")
    {
        if (!VivoxService.Instance.IsLoggedIn) return;
        try
        {
            await VivoxService.Instance.JoinGroupChannelAsync(textChannelName, ChatCapability.TextOnly);

            Debug.Log("Te uniste al canal : " + textChannelName);
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
            await VivoxService.Instance.JoinGroupChannelAsync(textChannelName, ChatCapability.AudioOnly);

           // await VivoxService.Instance.JoinEchoChannelAsync(textChannelName, ChatCapability.AudioOnly);

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
        var channelName = message.ChannelName;
        var senderName = message.SenderDisplayName;
        var senderId = message.SenderPlayerId;
        var messageText = message.MessageText;
        var timeRecived = message.ReceivedTime;

        print("Ch: " + channelName + " T:" + timeRecived + "| " + messageText);
    }

    private void OnMessageRecived(VivoxMessage message)
    {
        var channelName = message.ChannelName;
        var senderName = message.SenderDisplayName;
        var senderId =  message.SenderPlayerId;
        var messageText = message.MessageText;
        var timeRecived = message.ReceivedTime;

        print(senderName + "->  Ch: " + channelName + " T:" + timeRecived + "| " + messageText);
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

    public void SetParticipantVolume(string channelID, string username , int volume = 15)// -50  +50
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

}