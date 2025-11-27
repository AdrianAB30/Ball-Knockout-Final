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

    [Header("Cámara Compartida (Local)")]
    [SerializeField] private Camera sharedCamera;

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
        if (NetworkManager.Singleton != null) 
            NetworkManager.Singleton.Shutdown();

        if (sharedCamera == null)
        {
            GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamObj != null)
            {
                sharedCamera = mainCamObj.GetComponent<Camera>();
            }
        }

        if (sharedCamera != null)
        {
            sharedCamera.gameObject.SetActive(true);
        }

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
    }

    private void SpawnLocalPlayer(InputDevice device, int teamId)
    {
        Transform spawnPoint = (teamId == 1) ? spawnPointP1 : spawnPointP2;

        string schemeToUse = null;

        if (device is Keyboard)
        {
            schemeToUse = (teamId == 1) ? "KeyboardLeft" : "KeyboardRight";
        }
        else if (device is Gamepad)
        {
            schemeToUse = "Gamepad";
        }
        else if (device is Touchscreen)
        {
            schemeToUse = "Touch";
        }

        var playerInput = PlayerInput.Instantiate(
            playerPrefab,
            controlScheme: schemeToUse, 
            pairWithDevice: device
        );

        Vector3 correctPosition = new Vector3(spawnPoint.position.x, spawnPoint.position.y, 0);
        playerInput.transform.position = correctPosition;
        playerInput.transform.rotation = Quaternion.identity;
        playerInput.transform.SetParent(null);

        SetupLocalPlayerComponents(playerInput.gameObject, teamId);
    }

    private void SetupLocalPlayerComponents(GameObject playerObj, int teamId)
    {
        var netRb = playerObj.GetComponent<Unity.Netcode.Components.NetworkRigidbody2D>();
        if (netRb != null) Destroy(netRb);

        var netTransform = playerObj.GetComponent<Unity.Netcode.Components.NetworkTransform>();
        if (netTransform != null) Destroy(netTransform);

        var netObj = playerObj.GetComponent<NetworkObject>();
        if (netObj != null) Destroy(netObj);

        Camera[] playerCams = playerObj.GetComponentsInChildren<Camera>();
        foreach (Camera cam in playerCams)
        {
            Destroy(cam.gameObject);
        }

        AudioListener[] listeners = playerObj.GetComponentsInChildren<AudioListener>();
        foreach (AudioListener listener in listeners)
        {
            Destroy(listener);
        }

        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; 
            rb.simulated = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        playerObj.layer = LayerMask.NameToLayer("Default");
    }
}