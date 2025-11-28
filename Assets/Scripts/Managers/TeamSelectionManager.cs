using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;

public class TeamSelectionManager : MonoBehaviour
{
    [Header("Data Storage")]
    [SerializeField] private LocalMatchConfigurationSO matchData;

    [Header("Configuración Global")]
    [SerializeField] private GameConfigurationSO gameConfig;

    [Header("Slots Fila 1 (Jugador 1)")]
    [SerializeField] private Transform centerP1;
    [SerializeField] private Transform proP1;
    [SerializeField] private Transform noobP1;

    [Header("Slots Fila 2 (Jugador 2)")]
    [SerializeField] private Transform centerP2;
    [SerializeField] private Transform proP2;
    [SerializeField] private Transform noobP2;

    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Destinos de Luces (Targets)")]
    [SerializeField] private Transform targetProP1;
    [SerializeField] private Transform targetNoobP1;
    [SerializeField] private Transform targetProP2;
    [SerializeField] private Transform targetNoobP2;

    [Header("Controladores de Luces")]
    [SerializeField] private TeamLightController lightProController; 
    [SerializeField] private TeamLightController lightNoobController;

    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    private List<TeamCursorController> _cursors = new List<TeamCursorController>();
    private bool _player2Joined = false;

    private int _p1Team = 0;
    private int _p2Team = 0;

    private void Start()
    {
        if (matchData != null) matchData.ResetData();
        if (startButton) startButton.interactable = false;

        if (playerPrefab == null)
        {
            var playerInputManager = FindFirstObjectByType<PlayerInputManager>();
            if (playerInputManager != null) playerPrefab = playerInputManager.playerPrefab;
        }

        if (lightProController != null) lightProController.ReturnToStart();
        if (lightNoobController != null) lightNoobController.ReturnToStart();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (!_player2Joined && _cursors.Count == 1)
            {
                SpawnPlayer2WithArrowKeys();
            }
        }
    }

    private void SpawnPlayer2WithArrowKeys()
    {
        if (playerPrefab == null || Keyboard.current == null) return;

        var playerInput = PlayerInput.Instantiate(
            playerPrefab,
            controlScheme: "KeyboardRight",
            pairWithDevice: Keyboard.current
        );

        _player2Joined = true;
        Debug.Log("Player 2 se unió con KeyboardRight");
    }

    public void OnPlayerJoined(PlayerInput input)
    {
        var cursor = input.GetComponent<TeamCursorController>();
        int playerIndex = _cursors.Count;
        _cursors.Add(cursor);

        cursor.Setup(this, playerIndex);

        Transform startPos = (playerIndex == 0) ? centerP1 : centerP2;
        if (startPos != null)
        {
            input.transform.SetParent(startPos, false);
            input.transform.localPosition = Vector3.zero;
        }
    }

    public Transform GetTargetSlot(int playerIndex, int teamId)
    {
        if (playerIndex == 0) 
        {
            if (teamId == 0) return centerP1;
            if (teamId == 1) return proP1;
            if (teamId == 2) return noobP1;
        }
        else 
        {
            if (teamId == 0) return centerP2;
            if (teamId == 1) return proP2;
            if (teamId == 2) return noobP2;
        }
        return null;
    }
    public void UpdatePlayerLight(int playerIndex, int newTeamId)
    {
        if (playerIndex == 0) _p1Team = newTeamId;
        else if (playerIndex == 1) _p2Team = newTeamId;

        UpdateTeamLightState(lightProController, 1, targetProP1, targetProP2);

        UpdateTeamLightState(lightNoobController, 2, targetNoobP1, targetNoobP2);
    }

    private void UpdateTeamLightState(TeamLightController lightCtrl, int targetTeamId, Transform targetP1, Transform targetP2)
    {
        if (lightCtrl == null) return;

        bool p1IsIn = (_p1Team == targetTeamId);
        bool p2IsIn = (_p2Team == targetTeamId);

        if (!p1IsIn && !p2IsIn)
        {
            lightCtrl.ReturnToStart();
        }
        else
        {
            Transform target = p1IsIn ? targetP1 : targetP2;

            lightCtrl.MoveToTarget(target);
        }
    }
    public void CheckReadyState()
    {
        int teamProCount = 0;
        int teamNoobCount = 0;

        foreach (var cursor in _cursors)
        {
            if (cursor.CurrentTeam == 1) teamProCount++;
            if (cursor.CurrentTeam == 2) teamNoobCount++;
        }

        bool canStart = (teamProCount == 1 && teamNoobCount == 1);
        if (startButton) startButton.interactable = canStart;
    }

    public void AttemptStartGame()
    {
        CheckReadyState(); 
        if (startButton != null && startButton.interactable)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        if (matchData == null || gameConfig == null)
        {
            Debug.LogError("Faltan asignar SOs en el Inspector.");
            return;
        }

        gameConfig.SetLocalMode();

        foreach (var cursor in _cursors)
        {
            if (cursor.CurrentTeam != 0)
            {
                matchData.AddPlayerToTeam(cursor.Device, cursor.CurrentTeam);
            }
        }

        SceneManager.LoadScene(gameSceneName);
    }
}