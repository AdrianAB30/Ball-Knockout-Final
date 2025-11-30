using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem; 

public class GameSetupManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawns")]
    [SerializeField] private Transform spawnPointP1;
    [SerializeField] private Transform spawnPointP2;
    private int _spawnedPlayerCount = 0;

    [Header("Data")]
    [SerializeField] private LocalMatchConfigurationSO localMatchData;

    private void Start()
    {
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlayBattleMusic();
        }

        var gameManager = Object.FindFirstObjectByType<GameManager>(); 
        if (gameManager != null)
        {
            Canvas[] canvases = gameManager.GetComponentsInChildren<Canvas>(true);
            foreach (var c in canvases)
            {
                c.gameObject.SetActive(false);
            }

            Camera menuCam = gameManager.GetComponentInChildren<Camera>();
            if (menuCam != null) menuCam.gameObject.SetActive(false);
        }

        if (gameConfig == null) return;

        switch (gameConfig.CurrentGameMode)
        {
            case GameModeType.OnlineMultiplayer:
                StartOnlineSession();
                break;

            case GameModeType.LocalSplitScreen:
                StartLocalSession();
                break;
        }
    }

    // --- MODO ONLINE ---
    private void StartOnlineSession()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        if (NetworkManager.Singleton.IsServer)
        {
            SpawnOnlinePlayer(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnOnlinePlayer(clientId);
        }
    }

    private void SpawnOnlinePlayer(ulong clientId)
    {
        Transform spawnPoint = (clientId == 0) ? spawnPointP1 : spawnPointP2;
        Vector3 spawnPos = new Vector3(spawnPoint.position.x, spawnPoint.position.y, 0);

        GameObject p = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        p.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // --- MODO LOCAL ---
    private void StartLocalSession()
    {
        Debug.Log("Iniciando Modo Local Correcto...");

        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();

        if (localMatchData != null)
        {
            foreach (var playerData in localMatchData.Players)
            {
                SpawnLocalPlayer(playerData);
            }
        }
        else
        {
            Debug.LogError("No has asignado el LocalMatchConfigurationSO");
        }
    }

    private void SpawnLocalPlayer(LocalPlayerData data)
    {
        Transform spawnPoint = (data.TeamId == 1) ? spawnPointP1 : spawnPointP2;

        int playerNumber = data.PlayerIndex + 1;

        string schemeToUse = null;
        if (data.Device is Keyboard)
            schemeToUse = (playerNumber == 1) ? "KeyboardLeft" : "KeyboardRight";
        else if (data.Device is Gamepad)
            schemeToUse = "Gamepad";
        else if (data.Device is Touchscreen)
            schemeToUse = "Touch";

        var p = PlayerInput.Instantiate(
            playerPrefab,
            controlScheme: schemeToUse,
            pairWithDevice: data.Device
        );

        p.transform.position = spawnPoint.position;
        p.transform.rotation = spawnPoint.rotation;

        SetupLocalPlayerComponents(p.gameObject, playerNumber);

        var visuals = p.GetComponent<PlayerVisuals>();
        if (visuals != null)
        {
            visuals.SetPlayerInfo(playerNumber, data.TeamId);
        }
    }
    private void SpawnLocalPlayer(InputDevice device, int teamId, int playerNumber)
    {
        Transform spawnPoint = (teamId == 1) ? spawnPointP1 : spawnPointP2;

        string schemeToUse = null;
        if (device is Keyboard)
            schemeToUse = (playerNumber == 1) ? "KeyboardLeft" : "KeyboardRight";
        else if (device is Gamepad) schemeToUse = "Gamepad";
        else if (device is Touchscreen) schemeToUse = "Touch";

        var p = PlayerInput.Instantiate(
            playerPrefab,
            controlScheme: schemeToUse,
            pairWithDevice: device
        );

        p.transform.position = spawnPoint.position;
        p.transform.rotation = spawnPoint.rotation;

        SetupLocalPlayerComponents(p.gameObject, playerNumber);

        var visuals = p.GetComponent<PlayerVisuals>();
        if (visuals != null)
        {
            visuals.SetPlayerInfo(playerNumber, teamId);
        }
    }

    private void SetupLocalPlayerComponents(GameObject playerObj, int playerIndex)
    {
        var netRb = playerObj.GetComponent<Unity.Netcode.Components.NetworkRigidbody2D>();
        if (netRb != null) Destroy(netRb);

        var netTransform = playerObj.GetComponent<Unity.Netcode.Components.NetworkTransform>();
        if (netTransform != null) Destroy(netTransform);

        var netObj = playerObj.GetComponent<NetworkObject>();
        if (netObj != null) Destroy(netObj);

        Camera[] internalCameras = playerObj.GetComponentsInChildren<Camera>();
        foreach (var c in internalCameras) Destroy(c.gameObject);

        AudioListener[] listeners = playerObj.GetComponentsInChildren<AudioListener>();
        foreach (var l in listeners) Destroy(l);

        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
        }
    }
}