using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System;

public class PlayerListItemUI : MonoBehaviour
{
    public static event Action<string> OnKickPlayerRequested;


    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI readyIndicatorText;
    [SerializeField] private Button kickButton;
    [SerializeField] private GameObject crownImage;
    private Player _player;

    [Header("Voice UI")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private GameObject volumeSliderContainer;

    public void SetPlayerData(Player player, bool isHost)
    {
        _player = player;

        string playerName = $"Player {player.Id.Substring(0, 6)}";
        if (player.Data != null && player.Data.TryGetValue(LobbyManager.KEY_PLAYER_NAME, out PlayerDataObject nameData))
        {
            playerName = nameData.Value;
        }

        if (player.Id == AuthenticationService.Instance.PlayerId)
        {
            playerNameText.text = $"{PlayerAccountManager.Instance.PlayerName} (YOU)";
        }
        else
        {
            playerNameText.text = playerName;
        }

        if (player.Id == AuthenticationService.Instance.PlayerId)
        {
            playerNameText.text = $"{PlayerAccountManager.Instance.PlayerName} (YOU)";
            if (volumeSliderContainer != null) volumeSliderContainer.SetActive(false);
        }
        else
        {
            playerNameText.text = playerName;
            SetupVolumeSlider();
        }


        bool isReady = false;
        if (player.Data != null && player.Data.TryGetValue(LobbyManager.KEY_PLAYER_READY, out PlayerDataObject readyData))
        {
            bool.TryParse(readyData.Value, out isReady);
        }
        if (readyIndicatorText != null)
        {
            readyIndicatorText.text = isReady ? "READY" : "WAIT";
            readyIndicatorText.color = isReady ? Color.green : Color.red;
        }

        string lobbyHostId = LobbyManager.Instance.JoinedLobby?.HostId;
        bool isThisPlayerTheHost = !string.IsNullOrEmpty(lobbyHostId) && player.Id == lobbyHostId;

        if (crownImage != null)
        {
            crownImage.gameObject.SetActive(isThisPlayerTheHost);
        }

        bool canKick = isHost && !isThisPlayerTheHost;
        kickButton.gameObject.SetActive(canKick);
    }

    private void SetupVolumeSlider()
    {
        if (volumeSliderContainer == null || VivoxManager.Instance == null) return;
        
        volumeSliderContainer.SetActive(true);
        volumeSlider.minValue = -50;
        volumeSlider.maxValue = 20;
        volumeSlider.value = 0; 
        
        volumeSlider.onValueChanged.RemoveAllListeners();
        volumeSlider.onValueChanged.AddListener(OnParticipantVolumeChanged);
    }
    private void OnParticipantVolumeChanged(float value) 
    {
        if (_player != null && VivoxManager.Instance != null)
        {

            VivoxManager.Instance.SetParticipantVolume(_player.Id, (int)value);
        }
    }
    void OnEnable()
    {
        LobbyManager.OnKickPlayerFailed += ReactivateButton;
    }

    void OnDisable()
    {
        LobbyManager.OnKickPlayerFailed -= ReactivateButton;
    }

    void Start()
    {
        if (kickButton != null)
        {
            kickButton.onClick.AddListener(OnKickPlayerClicked);
        }
    }

    private void ReactivateButton()
    {
        if (gameObject.activeInHierarchy && kickButton != null)
        {
            kickButton.interactable = true;
        }
    }

    private void OnKickPlayerClicked()
    {
        if (_player != null)
        {
            if (kickButton != null) kickButton.interactable = false;
            OnKickPlayerRequested?.Invoke(_player.Id);
        }
    }
}