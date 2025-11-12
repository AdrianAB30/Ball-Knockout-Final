using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System; 
using Unity.Services.Authentication;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject panelChangeName;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject buttonsMenu;
    [SerializeField] private GameObject loginButtonsPanel;
    [SerializeField] private GameObject createLobbyGroup;
    [SerializeField] private GameObject joinLobbyGroup;

    [Header("Service Dependencies")]
    [SerializeField] private FadeManager fadeManager;
    [SerializeField] private AnonymousAuthService anonymousAuthService;
    [SerializeField] private UnityAccountAuthService unityAccountAuthService;


    private void Start()
    {
        loginButtonsPanel.SetActive(false);
        statusText.text = "Initializing Services...";
        statusText.text = "Ready to login";
        loginButtonsPanel.SetActive(true);
    }

    private void OnEnable()
    {
        anonymousAuthService.OnSignedIn.AddListener(HandleLoginSuccess_Guest);
        anonymousAuthService.OnSignInFailed.AddListener(HandleLoginFailed);

        unityAccountAuthService.OnSignedIn.AddListener(HandleLoginSuccess_Unity);
        unityAccountAuthService.OnSignInFailed.AddListener(HandleLoginFailed);
    }

    private void OnDisable()
    {
        anonymousAuthService.OnSignedIn.RemoveListener(HandleLoginSuccess_Guest);
        anonymousAuthService.OnSignInFailed.RemoveListener(HandleLoginFailed);

        unityAccountAuthService.OnSignedIn.RemoveListener(HandleLoginSuccess_Unity);
        unityAccountAuthService.OnSignInFailed.RemoveListener(HandleLoginFailed);
    }

    public async void OnClick_LoginWithUnity()
    {
        loginButtonsPanel.SetActive(false);
        statusText.text = "Logging in with Unity...";
        try
        {
            await unityAccountAuthService.SignInAsync();
            buttonsMenu.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LoginWithUnity Tarea cancelada o fallida: {e.Message}");
        }
    }

    public async void OnClick_LoginAsGuest()
    {
        loginButtonsPanel.SetActive(false);
        statusText.text = "Logging in as Guest...";
        try
        {
            await anonymousAuthService.SignInAsync();
            buttonsMenu.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LoginAsGuest Tarea cancelada o fallida: {e.Message}");
        }
    }

    private async void HandleLoginSuccess_Guest(PlayerInfo info)
    {
        await PlayerAccountManager.Instance.OnLoginSuccess(isGuest: true);
        OnLoginSuccessUIUpdate();
    }

    private async void HandleLoginSuccess_Unity(PlayerInfo info)
    {
        await PlayerAccountManager.Instance.OnLoginSuccess(isGuest: false);
        OnLoginSuccessUIUpdate();
    }

    private void HandleLoginFailed(Exception e)
    {
        statusText.text = "Login failed. Try again.";
        Debug.LogError($"Login Failed: {e.Message}");
        loginButtonsPanel.SetActive(true);
    }

    private void OnLoginSuccessUIUpdate()
    {
        playerNameText.text = PlayerAccountManager.Instance.PlayerName;
        statusText.text = "Welcome, " + PlayerAccountManager.Instance.PlayerName;
        fadeManager.StartFadeTransition();
    }

    public async void ChangePlayerName()
    {
        string newName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            statusText.text = "Enter a valid name.";
            return;
        }

        if (newName.Contains(" "))
        {
            statusText.text = "El nombre no puede contener espacios.";
            return;
        }

        statusText.text = "Updating name...";

        try
        {
            string updatedName = await PlayerAccountManager.Instance.ChangePlayerName(newName);

            playerNameText.text = updatedName;
            statusText.text = "Name updated!";
            panelChangeName.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to change name: {e.Message}");
            if (e.Message.Contains("Invalid Player name"))
            {
                statusText.text = "Nombre inválido (no uses espacios).";
            }
            else
            {
                statusText.text = "Error: Could not change name.";
            }
        }
    }

    public void GenerateRandomName()
    {
        nameInputField.text = PlayerAccountManager.Instance.GenerateRandomName();
    }

    public void ToggleChangeNamePanel()
    {
        panelChangeName.SetActive(!panelChangeName.activeSelf);
        if (panelChangeName.activeSelf)
        {
            nameInputField.text = PlayerAccountManager.Instance.PlayerName;
        }
    }

    public void ToggleLobbyPanel()
    {
        lobbyPanel.SetActive(!lobbyPanel.activeSelf);
        if (lobbyPanel.activeSelf)
        {
            buttonsMenu.SetActive(false);
        }
        else
        {
            buttonsMenu.SetActive(true);
        }
    }
    public void ToggleCreateLobbyGroup()
    {
        createLobbyGroup.SetActive(true);
        joinLobbyGroup.SetActive(false);
    }
    public void ToggleJoinLobbyGroup()
    {
        createLobbyGroup.SetActive(false);
        joinLobbyGroup.SetActive(true);
    }
    public void ExitGame()
    {
        Application.Quit();
    }
}