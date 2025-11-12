using UnityEngine;
using UnityEngine.UI;
using System; 

public class LobbyTabManager : MonoBehaviour
{
    [Header("Botones de Pestañas")]
    [SerializeField] private Button joinTabButton;
    [SerializeField] private Button createTabButton;

    [Header("Grupos de Contenido (Paneles)")]
    [SerializeField] private GameObject joinLobbyGroup;
    [SerializeField] private GameObject createLobbyGroup;

    [SerializeField] private GameObject lobbyCreatedGroup; 

    void OnEnable()
    {
        LobbyManager.OnLobbyJoinedOrLeft += CheckLobbyState;
    }

    void OnDisable()
    {
        LobbyManager.OnLobbyJoinedOrLeft -= CheckLobbyState;
    }

    void Start()
    {
        if (joinTabButton != null)
            joinTabButton.onClick.AddListener(ShowJoinPanel);

        if (createTabButton != null)
            createTabButton.onClick.AddListener(ShowCreatePanel);

        ShowJoinPanel();
        CheckLobbyState();
    }

    private void CheckLobbyState()
    {
        bool inLobby = LobbyManager.Instance.JoinedLobby != null;

        if (inLobby)
        {
            joinLobbyGroup.SetActive(false);
            createLobbyGroup.SetActive(false);
            lobbyCreatedGroup.SetActive(true); 

            joinTabButton.gameObject.SetActive(false);
            createTabButton.gameObject.SetActive(false);
        }
        else
        {
            lobbyCreatedGroup.SetActive(false);

            joinTabButton.gameObject.SetActive(true);
            createTabButton.gameObject.SetActive(true);

            ShowJoinPanel();
        }
    }

    public void ShowJoinPanel()
    {
        if (joinLobbyGroup != null) joinLobbyGroup.SetActive(true);
        if (createLobbyGroup != null) createLobbyGroup.SetActive(false);

        if (joinTabButton != null) joinTabButton.interactable = false;
        if (createTabButton != null) createTabButton.interactable = true;
    }

    public void ShowCreatePanel()
    {
        if (joinLobbyGroup != null) joinLobbyGroup.SetActive(false);
        if (createLobbyGroup != null) createLobbyGroup.SetActive(true);

        if (joinTabButton != null) joinTabButton.interactable = true;
        if (createTabButton != null) createTabButton.interactable = false;
    }
}