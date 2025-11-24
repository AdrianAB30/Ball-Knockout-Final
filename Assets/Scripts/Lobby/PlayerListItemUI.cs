using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;

public class PlayerListItemUI : MonoBehaviour
{
    public static event Action<string> OnKickPlayerRequested;

    [Header("Player UI")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI readyIndicatorText;
    [SerializeField] private Button kickButton;
    [SerializeField] private GameObject crownImage;

    [Header("Voice UI")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private GameObject volumeSliderContainer;
    [SerializeField] private GameObject speakerIcon;
    [SerializeField] private TextMeshProUGUI volumeValueText; 

    [Header("Mute Button UI")]
    [SerializeField] private Button muteButton;
    [SerializeField] private Image muteButtonImage;
    [SerializeField] private Sprite unmutedIcon;
    [SerializeField] private Sprite mutedIcon;

    [Header("Avatar UI")]
    [SerializeField] private Image playerAvatarImage; 
    [SerializeField] private List<Sprite> avatarBank;

    private Player _player;
    private bool _isLocalPlayer;

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

    private void Update()
    {
        if (_player == null || speakerIcon == null || VivoxManager.Instance == null) return;

        if (_isLocalPlayer)
        {
            if (speakerIcon.activeSelf)
                speakerIcon.SetActive(false);
            return; 
        }
        bool isSpeaking = VivoxManager.Instance.IsPlayerSpeaking(_player.Id);

        if (speakerIcon.activeSelf != isSpeaking)
        {
            speakerIcon.SetActive(isSpeaking);
        }
    }
    public void SetPlayerData(Player player, bool isHost)
    {
        _player = player;

        _isLocalPlayer = player.Id == AuthenticationService.Instance.PlayerId;

        string playerName = $"Player {player.Id.Substring(0, 6)}";
        if (player.Data != null && player.Data.TryGetValue(LobbyManager.KEY_PLAYER_NAME, out PlayerDataObject nameData))
        {
            playerName = nameData.Value;
        }

        if (_isLocalPlayer)
        {
            playerNameText.text = $"{PlayerAccountManager.Instance.PlayerName} (YOU)";

            if (volumeSliderContainer != null) volumeSliderContainer.SetActive(false);
            if (volumeValueText != null) volumeValueText.gameObject.SetActive(false);
        }
        else
        {
            playerNameText.text = playerName;

            if (volumeValueText != null) volumeValueText.gameObject.SetActive(true);

            SetupVolumeSlider();
            int savedVolume = VivoxManager.Instance.GetSavedVolume(player.Id);
            OnParticipantVolumeChanged(savedVolume);
        }

        if (playerAvatarImage != null && avatarBank != null && avatarBank.Count > 0)
        {
            if (player.Data != null && player.Data.TryGetValue(LobbyManager.KEY_AVATAR_ID, out PlayerDataObject avatarData))
            {
                if (int.TryParse(avatarData.Value, out int avatarIndex))
                {
                    if (avatarIndex >= 0 && avatarIndex < avatarBank.Count)
                    {
                        playerAvatarImage.sprite = avatarBank[avatarIndex];
                    }
                }
            }
            else
            {
                playerAvatarImage.sprite = avatarBank[0];
            }
        }
        SetupMuteButton();

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

        volumeSlider.onValueChanged.RemoveAllListeners();

        int savedVolume = VivoxManager.Instance.GetSavedVolume(_player.Id);
        volumeSlider.SetValueWithoutNotify(savedVolume);

        UpdateVolumeLabel(savedVolume);

        volumeSlider.onValueChanged.AddListener(OnParticipantVolumeChanged);
    }

    private void OnParticipantVolumeChanged(float value)
    {
        if (_player != null && VivoxManager.Instance != null)
        {
            VivoxManager.Instance.SetParticipantVolume(_player.Id, (int)value);
            UpdateVolumeLabel(value);
        }
    }

    private void UpdateVolumeLabel(float value)
    {
        if (volumeValueText == null) return;

        float range = volumeSlider.maxValue - volumeSlider.minValue;
        float percentage = ((value - volumeSlider.minValue) / range) * 100f;

        volumeValueText.text = $"{Mathf.RoundToInt(percentage)}%";
    }

    private void SetupMuteButton()
    {
        if (muteButton == null) return;

        muteButton.onClick.RemoveAllListeners();
        muteButton.onClick.AddListener(OnMuteButtonClicked);

        bool isMuted = false;

        if (_isLocalPlayer)
        {
            if (VivoxManager.Instance != null)
                isMuted = VivoxManager.Instance.IsMuted;
        }
        else
        {
            if (VivoxManager.Instance != null)
                isMuted = VivoxManager.Instance.GetSavedMuteState(_player.Id);
        }

        UpdateMuteIcon(isMuted);
    }
    private void OnMuteButtonClicked()
    {
        if (VivoxManager.Instance == null) return;
        bool newMuteState = false;

        if (_isLocalPlayer)
        {
            VivoxManager.Instance.ToggleMute();
            newMuteState = VivoxManager.Instance.IsMuted;
        }
        else
        {
            newMuteState = VivoxManager.Instance.ToggleParticipantMute(_player.Id);
        }

        UpdateMuteIcon(newMuteState);
    }

    private void UpdateMuteIcon(bool isMuted)
    {
        if (muteButtonImage == null || unmutedIcon == null || mutedIcon == null) return;
        muteButtonImage.sprite = isMuted ? mutedIcon : unmutedIcon;
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