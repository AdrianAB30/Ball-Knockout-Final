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

    private void StartOnlineSession()
    {
        Debug.Log("Iniciando Modo Online...");
    }

    // --- MODO LOCAL (PANTALLA DIVIDIDA) ---
    private void StartLocalSession()
    {
        Debug.Log("Iniciando Modo Local...");

        if (Camera.main != null) Camera.main.gameObject.SetActive(false);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        GameObject p1 = Instantiate(playerPrefab, spawnPointP1.position, spawnPointP1.rotation);
        SetupLocalPlayer(p1, 1); 

        GameObject p2 = Instantiate(playerPrefab, spawnPointP2.position, spawnPointP2.rotation);
        SetupLocalPlayer(p2, 2); 
    }

    private void SetupLocalPlayer(GameObject playerObj, int playerIndex)
    {

        Destroy(playerObj.GetComponent<NetworkObject>());
        Destroy(playerObj.GetComponent<Unity.Netcode.Components.NetworkTransform>());

        Camera cam = playerObj.GetComponentInChildren<Camera>();
        AudioListener listener = playerObj.GetComponentInChildren<AudioListener>();

        if (playerIndex == 1)
        {
            cam.rect = new Rect(0, 0.5f, 1, 0.5f);
        }
        else
        {
            cam.rect = new Rect(0, 0, 1, 0.5f);
            if (listener) Destroy(listener);
        }

        PlayerInput input = playerObj.GetComponent<PlayerInput>();

        // Esto es un ejemplo básico. Lo ideal es usar el PlayerInputManager,
        // pero para forzar controles rápidos en PC:
        if (playerIndex == 1)
        {
            input.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
        }
        else
        {
            // P2 usa el primer Gamepad conectado
            if (Gamepad.all.Count > 0)
                input.SwitchCurrentControlScheme("Gamepad", Gamepad.all[0]);
            else
                Debug.LogWarning("No hay Gamepad para el Player 2");
        }
    }
}