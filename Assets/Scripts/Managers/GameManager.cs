using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;

public class GameManager : PersistentSingleton<GameManager>
{
    [Header("UI References")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject[] playerName;
    [SerializeField] private GameObject mainMenuCanvas;

    // PANELES
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
            Debug.Log("GameManager: Sesión restaurada automáticamente.");
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
                if (lastType == "Unity") await unityAccountAuthService.SignInAsync();
                else await anonymousAuthService.SignInAsync();
                return;
            }
            catch (Exception e)
            {
                PlayerPrefs.DeleteKey("LastLoginType");
                PlayerPrefs.Save();
                if (UnityServices.State == ServicesInitializationState.Initialized) AuthenticationService.Instance.SignOut();
            }
        }

        statusText.text = "Ready to login";
        loginButtonsPanel.SetActive(true);
    }

    public void OnProfileUpdated(UserProfileData data)
    {
        if (playerNameText != null) playerNameText.text = PlayerAccountManager.Instance.PlayerName;
        if (statusText != null) statusText.text = "Welcome, " + PlayerAccountManager.Instance.PlayerName;
    }

    public async void OnClick_LoginWithUnity()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClick();

        loginButtonsPanel.SetActive(false);
        statusText.text = "Logging in with Unity...";
        try
        {
            await unityAccountAuthService.SignInAsync();
            buttonsMenu.SetActive(true);
        }
        catch (Exception e) { Debug.LogWarning($"LoginWithUnity Fallido: {e.Message}"); }
    }

    public async void OnClick_LoginAsGuest()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClick();

        loginButtonsPanel.SetActive(false);
        statusText.text = "Logging in as Guest...";
        try
        {
            await anonymousAuthService.SignInAsync();
            buttonsMenu.SetActive(true);
        }
        catch (Exception e) { Debug.LogWarning($"LoginAsGuest Fallido: {e.Message}"); }
    }

    private async void HandleLoginSuccess_Guest(PlayerInfo info)
    {
        if (AudioManager.Instance) AudioManager.Instance.PlaySuccess();

        await PlayerAccountManager.Instance.OnLoginSuccess(isGuest: true);
        if (VivoxManager.Instance != null) _ = VivoxManager.Instance.LoginVivox();
        OnLoginSuccessUIUpdate();
    }

    private async void HandleLoginSuccess_Unity(PlayerInfo info)
    {
        if (AudioManager.Instance) AudioManager.Instance.PlaySuccess();

        await PlayerAccountManager.Instance.OnLoginSuccess(isGuest: false);
        if (VivoxManager.Instance != null) _ = VivoxManager.Instance.LoginVivox();
        OnLoginSuccessUIUpdate();
    }

    private void HandleLoginFailed(Exception e)
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayError();

        statusText.text = "Login failed. Try again.";
        loginButtonsPanel.SetActive(true);
    }

    private void OnLoginSuccessUIUpdate()
    {
        playerNameText.text = PlayerAccountManager.Instance.PlayerName;
        fadeManager.StartFadeTransition();
    }

    private void SetMainMenuVisuals(bool isActive)
    {
        buttonsMenu.SetActive(isActive);
        editProfileButton.SetActive(isActive);
        for (int i = 0; i < playerName.Length; i++)
        {
            playerName[i].SetActive(isActive);
        }
    }

    private void HandlePanelToggle(GameObject panel)
    {
        bool isOpening = !panel.activeSelf;

        if (isOpening)
        {
            if (AudioManager.Instance) AudioManager.Instance.PlayPanelOpen();

            panel.SetActive(true);
            SetMainMenuVisuals(false);

            if (panel == panelChangeName)
            {
                nameInputField.text = PlayerAccountManager.Instance.PlayerName;
            }
        }
        else
        {
            SetMainMenuVisuals(true);

            if (AudioManager.Instance) AudioManager.Instance.PlayPanelClose();

            JuicyPanel juicy = panel.GetComponent<JuicyPanel>();
            if (juicy != null)
            {
                juicy.ClosePanel();
            }
            else
            {
                panel.SetActive(false);
            }
        }
    }

    public void ToggleChangeNamePanel()
    {
        HandlePanelToggle(panelChangeName);
    }

    public void ToggleLobbyPanel()
    {
        HandlePanelToggle(lobbyPanel);
    }

    public void ToggleSettingsPanel()
    {
        HandlePanelToggle(settingsPanel);
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