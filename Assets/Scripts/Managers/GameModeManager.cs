using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System;

public class GameModeManager : NetworkBehaviour
{
    public static GameModeManager Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private float timeToRespawn;

    public event Action<int> OnCountdownStart;
    public event Action OnRoundStart;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (gameConfig.CurrentGameMode == GameModeType.LocalSplitScreen)
        {
            StartCoroutine(StartRoundSequence());
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(StartRoundSequence());
        }
    }

    private void OnEnable() { Health.OnPlayerDied += HandlePlayerDeath; }
    private void OnDisable() { Health.OnPlayerDied -= HandlePlayerDeath; }

    private void HandlePlayerDeath(int playerIndex)
    {
        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsServer) return;

        Debug.Log("Jugador murió. Reiniciando ronda...");
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(timeToRespawn);

        ReviveAllPlayers();
        ResetPlayerPositions();

        yield return StartCoroutine(StartRoundSequence());
    }

    private IEnumerator StartRoundSequence()
    {
        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer)
        {
            SetMovementClientRpc(false);
        }
        else
        {
            SetPlayersMovement(false);
        }

        yield return new WaitForSeconds(0.5f); 

        if (AudioManager.Instance) AudioManager.Instance.PlayCountdown();
        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer) PlaySoundClientRpc("countdown");

        for (int i = 3; i > 0; i--)
        {
            OnCountdownStart?.Invoke(i);

            if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer) UpdateCountdownClientRpc(i);

            yield return new WaitForSeconds(1f);

            if (AudioManager.Instance) AudioManager.Instance.PlayClick();
            if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer) PlaySoundClientRpc("click");
        }

        OnRoundStart?.Invoke();
        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer) ShowGoClientRpc();

        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlayBattleMusic();
        }
        else
        {
            Debug.LogError("No encuentro el AudioManager en la GameScene!");
        }

        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer) PlaySoundClientRpc("go_music");

        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer)
        {
            SetMovementClientRpc(true);
        }
        else
        {
            SetPlayersMovement(true);
        }
    }

    // --- RPCs PARA ONLINE ---

    [ClientRpc]
    private void SetMovementClientRpc(bool canMove)
    {
        SetPlayersMovement(canMove);
    }

    [ClientRpc]
    private void UpdateCountdownClientRpc(int number)
    {
        if (IsServer) return; 
        OnCountdownStart?.Invoke(number);
    }

    [ClientRpc]
    private void ShowGoClientRpc()
    {
        if (IsServer) return;
        OnRoundStart?.Invoke();
    }

    [ClientRpc]
    private void PlaySoundClientRpc(string type)
    {
        if (IsServer) return; 
        if (AudioManager.Instance == null) return;

        if (type == "countdown") AudioManager.Instance.PlayCountdown();
        if (type == "click") AudioManager.Instance.PlayClick();
        if (type == "go_music")
        {
            AudioManager.Instance.PlayBattleMusic();
        }
    }
    private void SetPlayersMovement(bool canMove)
    {
        var movements = FindObjectsByType<MovementController>(FindObjectsSortMode.None);
        var dashes = FindObjectsByType<DashController>(FindObjectsSortMode.None);

        foreach (var move in movements) move.SetDead(!canMove);
        foreach (var dash in dashes) dash.SetDead(!canMove);
    }

    private void ReviveAllPlayers()
    {
        var healths = FindObjectsByType<Health>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Debug.Log($"Reviviendo a {healths.Length} jugadores...");

        foreach (var h in healths)
        {
            h.Revive();
        }
    }

    private void ResetPlayerPositions()
    {
        Transform s1 = GameObject.Find("Spawn P1")?.transform;
        Transform s2 = GameObject.Find("Spawn P2")?.transform;
        if (!s1 || !s2) return;

        var visuals = FindObjectsByType<PlayerVisuals>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var v in visuals)
        {
            var move = v.GetComponent<MovementController>();

            int teamId = v.CurrentTeamId; 

            if (move)
            {
                if (teamId == 1) move.TeleportTo(s1.position, s1.rotation);
                else move.TeleportTo(s2.position, s2.rotation);
            }
        }
    }
}