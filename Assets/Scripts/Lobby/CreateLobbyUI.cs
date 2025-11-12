using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class CreateLobbyUI : MonoBehaviour
{
    public static event Action<string, int> OnCreateLobbyRequested;

    [Header("UI References")]
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private int maxPlayers = 4;

    void Start()
    {
        if (createLobbyButton != null)
        {
            createLobbyButton.onClick.AddListener(OnCreateLobbyButtonClicked);
        }
    }

    void OnEnable()
    {
        LobbyManager.OnCreateLobbyFailed += ReactivateButton;
        ReactivateButton();
    }

    void OnDisable()
    {
        LobbyManager.OnCreateLobbyFailed -= ReactivateButton;
    }

    private void ReactivateButton()
    {
        if (createLobbyButton != null) createLobbyButton.interactable = true;
    }

    public void OnCreateLobbyButtonClicked()
    {
        string lobbyName = lobbyNameInput.text;
        if (string.IsNullOrEmpty(lobbyName))
        {
            Debug.LogWarning("El nombre del lobby no puede estar vacío.");
            return;
        }

        createLobbyButton.interactable = false;
        OnCreateLobbyRequested?.Invoke(lobbyName, maxPlayers);
    }
}