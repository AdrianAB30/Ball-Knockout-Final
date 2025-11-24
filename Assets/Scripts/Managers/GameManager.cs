using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System; 
using Unity.Services.Authentication;
using Unity.Services.Core;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject[] playerName;
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject panelChangeName;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject buttonsMenu;
    [SerializeField] private GameObject loginButtonsPanel;
    [SerializeField] private GameObject createLobbyGroup;
    [SerializeField] private GameObject joinLobbyGroup;
    [SerializeField] private GameObject editProfileButton;

    [Header("Service Dependencies")]
    [SerializeField] private FadeManager fadeManager;
    [SerializeField] private AnonymousAuthService anonymousAuthService;
    [SerializeField] private UnityAccountAuthService unityAccountAuthService;

    private void OnEnable()
    {
        anonymousAuthService.OnSignedIn.AddListener(HandleLoginSuccess_Guest);
        anonymousAuthService.OnSignInFailed.AddListener(HandleLoginFailed);

        unityAccountAuthService.OnSignedIn.AddListener(HandleLoginSuccess_Unity);
        unityAccountAuthService.OnSignInFailed.AddListener(HandleLoginFailed);

        PlayerAccountManager.OnProfileLoaded += OnProfileUpdated;
    }

    private void OnDisable()
    {
        anonymousAuthService.OnSignedIn.RemoveListener(HandleLoginSuccess_Guest);
        anonymousAuthService.OnSignInFailed.RemoveListener(HandleLoginFailed);

        unityAccountAuthService.OnSignedIn.RemoveListener(HandleLoginSuccess_Unity);
        unityAccountAuthService.OnSignInFailed.RemoveListener(HandleLoginFailed);

        PlayerAccountManager.OnProfileLoaded -= OnProfileUpdated;
    }
    private async void Start()
    {
        loginButtonsPanel.SetActive(false);
        statusText.text = "Initializing Services...";

        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error inicializando Unity Services: {e.Message}");
            statusText.text = "Init Failed";
            return;
        }
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("GameManager: Sesión restaurada automáticamente. Saltando login manual.");

            bool wasGuest = PlayerPrefs.GetString("LastLoginType") == "Guest";

            if (wasGuest) HandleLoginSuccess_Guest(AuthenticationService.Instance.PlayerInfo);
            else HandleLoginSuccess_Unity(AuthenticationService.Instance.PlayerInfo);

            return;
        }

        if (PlayerPrefs.HasKey("LastLoginType"))
        {
            string lastType = PlayerPrefs.GetString("LastLoginType");
            statusText.text = $"Auto-logging in as {lastType}...";

            try
            {
                if (lastType == "Unity")
                {
                    await unityAccountAuthService.SignInAsync();
                }
                else
                {
                    await anonymousAuthService.SignInAsync();
                }
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Auto-login falló ({e.Message}). Limpiando sesión...");

                PlayerPrefs.DeleteKey("LastLoginType");
                PlayerPrefs.Save();

                if (UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized)
                {
                    AuthenticationService.Instance.SignOut();
                }
            }
        }

        statusText.text = "Ready to login";
        loginButtonsPanel.SetActive(true);
    }

    public void OnProfileUpdated(UserProfileData data)
    {
        if (playerNameText != null)
        {
            playerNameText.text = PlayerAccountManager.Instance.PlayerName;
        }

        if (statusText != null)
        {
            statusText.text = "Welcome, " + PlayerAccountManager.Instance.PlayerName;
        }
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
        if (VivoxManager.Instance != null)
        {
            _ = VivoxManager.Instance.LoginVivox();
        }
        OnLoginSuccessUIUpdate();
    }

    private async void HandleLoginSuccess_Unity(PlayerInfo info)
    {
        await PlayerAccountManager.Instance.OnLoginSuccess(isGuest: false);
        if (VivoxManager.Instance != null)
        {
            _ = VivoxManager.Instance.LoginVivox();
        }
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
        fadeManager.StartFadeTransition();
    }
    public void ToggleChangeNamePanel()
    {
        panelChangeName.SetActive(!panelChangeName.activeSelf);
        buttonsMenu.SetActive(!panelChangeName.activeSelf);
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

            for (int i = 0; i < playerName.Length; i++)
            {
                playerName[i].SetActive(false);
            }
            editProfileButton.SetActive(false);

        }
        else
        {
            buttonsMenu.SetActive(true);
            for (int i = 0; i < playerName.Length; i++)
            {
                playerName[i].SetActive(true);
            }
            editProfileButton.SetActive(true);
        }
    }
    public void ToggleSettingsPanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
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