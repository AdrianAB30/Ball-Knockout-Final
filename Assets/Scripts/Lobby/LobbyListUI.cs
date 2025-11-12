using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using System; 

public class LobbyListUI : MonoBehaviour
{
    // Eventos que este script dispara
    public static event Action OnRefreshRequested;
    public static event Action OnQuickJoinRequested;
    public static event Action<string> OnJoinByCodeRequested;

    [Header("UI References")]
    [SerializeField] private Transform lobbyListContainer;
    [SerializeField] private GameObject lobbyListItemPrefab;
    [SerializeField] private TextMeshProUGUI noLobbiesFoundText;

    [Header("Botones del Panel")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;

    private List<GameObject> _spawnedLobbyItems = new List<GameObject>();

    void OnEnable()
    {
        LobbyManager.OnLobbyListChanged += UpdateLobbyListUI;

        // --- ¡CORRECCIÓN! Escucha los eventos del LobbyManager ---
        LobbyManager.OnQuickJoinFailed += OnJoinFailed;
        LobbyManager.OnJoinByCodeFailed += OnJoinFailed;
    }

    void OnDisable()
    {
        LobbyManager.OnLobbyListChanged -= UpdateLobbyListUI;

        // --- ¡CORRECCIÓN! Se desuscribe de los eventos del LobbyManager ---
        LobbyManager.OnQuickJoinFailed -= OnJoinFailed;
        LobbyManager.OnJoinByCodeFailed -= OnJoinFailed;
    }

    void Start()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        if (quickJoinButton != null) quickJoinButton.onClick.AddListener(OnQuickJoinButtonClicked);
        if (joinByCodeButton != null) joinByCodeButton.onClick.AddListener(OnJoinByCodeButtonClicked);
        OnRefreshButtonClicked();
    }

    void OnDestroy()
    {
        if (refreshButton != null) refreshButton.onClick.RemoveListener(OnRefreshButtonClicked);
        if (quickJoinButton != null) quickJoinButton.onClick.RemoveListener(OnQuickJoinButtonClicked);
        if (joinByCodeButton != null) joinByCodeButton.onClick.RemoveListener(OnJoinByCodeButtonClicked);
    }

    private void OnJoinFailed()
    {
        if (quickJoinButton != null) quickJoinButton.interactable = true;
        if (joinByCodeButton != null) joinByCodeButton.interactable = true;
    }

    private void ClearLobbyListUI()
    {
        foreach (GameObject item in _spawnedLobbyItems) Destroy(item);
        _spawnedLobbyItems.Clear();
    }

    private void UpdateLobbyListUI(List<Lobby> lobbyList)
    {
        ClearLobbyListUI();
        noLobbiesFoundText.gameObject.SetActive(lobbyList == null || lobbyList.Count == 0);

        if (lobbyList == null) return;

        foreach (Lobby lobby in lobbyList)
        {
            if (lobbyListItemPrefab == null || lobbyListContainer == null) continue;
            GameObject itemGO = Instantiate(lobbyListItemPrefab, lobbyListContainer);
            LobbyListItemUI itemUI = itemGO.GetComponent<LobbyListItemUI>();
            if (itemUI != null)
            {
                itemUI.SetLobbyData(lobby);
                _spawnedLobbyItems.Add(itemGO);
            }
        }
    }

    public void OnRefreshButtonClicked()
    {
        if (refreshButton != null) refreshButton.interactable = false;
        OnRefreshRequested?.Invoke();
        Invoke(nameof(ReEnableRefreshButton), 1f);
    }

    private void ReEnableRefreshButton()
    {
        if (refreshButton != null) refreshButton.interactable = true;
    }

    public void OnQuickJoinButtonClicked()
    {
        if (quickJoinButton != null) quickJoinButton.interactable = false;
        OnQuickJoinRequested?.Invoke();
    }

    public void OnJoinByCodeButtonClicked()
    {
        string code = joinCodeInputField.text;
        if (string.IsNullOrEmpty(code)) return;

        if (joinByCodeButton != null) joinByCodeButton.interactable = false;
        OnJoinByCodeRequested?.Invoke(code);
    }
}