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

    [Header("Data")]
    [SerializeField] private LocalMatchConfigurationSO localMatchData;

    private void Start()
    {
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
        Debug.Log("Iniciando Modo Online...");

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

        GameObject p = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        p.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // --- MODO LOCAL (PANTALLA DIVIDIDA) ---
    private void StartLocalSession()
    {
        Debug.Log("Iniciando Modo Local desde SO");

        if (Camera.main != null) Camera.main.gameObject.SetActive(false);
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();

        if (localMatchData != null)
        {
            foreach (var device in localMatchData.Team1Devices)
            {
                SpawnLocalPlayer(device, 1);
            }

            foreach (var device in localMatchData.Team2Devices)
            {
                SpawnLocalPlayer(device, 2);
            }
        }
        else
        {
            Debug.LogError("No has asignado el LocalMatchConfigurationSO en el GameSetupManager");
        }
    }
    private void SpawnLocalPlayer(InputDevice device, int teamId)
    {
        Transform spawnPoint = (teamId == 1) ? spawnPointP1 : spawnPointP2;

        string schemeToUse = null;

        if (device is Keyboard)
        {
            if (teamId == 1) schemeToUse = "KeyboardLeft";
            else schemeToUse = "KeyboardRight";
        }
        else if (device is Gamepad)
        {
            schemeToUse = "Gamepad";
        }
        else if (device is Touchscreen)
        {
            schemeToUse = "Touch";
        }

        var p = PlayerInput.Instantiate(
            playerPrefab,
            controlScheme: schemeToUse, 
            pairWithDevice: device
        );

        p.transform.position = spawnPoint.position;
        p.transform.rotation = spawnPoint.rotation;

        SetupLocalPlayerComponents(p.gameObject, teamId);
    }
    private void SetupLocalPlayerComponents(GameObject playerObj, int playerIndex)
    {
        var netRb = playerObj.GetComponent<Unity.Netcode.Components.NetworkRigidbody2D>();
        if (netRb != null) Destroy(netRb);

        var netTransform = playerObj.GetComponent<Unity.Netcode.Components.NetworkTransform>();
        if (netTransform != null) Destroy(netTransform);

        var netObj = playerObj.GetComponent<NetworkObject>();
        if (netObj != null) Destroy(netObj);

        Camera cam = playerObj.GetComponentInChildren<Camera>();
        AudioListener listener = playerObj.GetComponentInChildren<AudioListener>();

        if (cam != null)
        {
            if (playerIndex == 1)
            {
                cam.rect = new Rect(0, 0.5f, 1, 0.5f);
            }
            else
            {
                cam.rect = new Rect(0, 0, 1, 0.5f);
                if (listener) Destroy(listener);
            }
        }

        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; 
            rb.simulated = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }
}