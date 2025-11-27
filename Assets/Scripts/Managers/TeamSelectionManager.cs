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

    private List<TeamCursorController> _cursors = new List<TeamCursorController>();

    private void Start()
    {
        if (matchData != null) matchData.ResetData();
        if (startButton) startButton.interactable = false;

        if (proP1 == null || proP2 == null)
            Debug.LogError("¡CUIDADO! Faltan asignar los Slots en el Inspector del TeamSelectionManager.");
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
        if (proP1 == null || noobP1 == null || proP2 == null || noobP2 == null)
        {
            Debug.LogError("ERROR CRÍTICO: No se puede iniciar. Faltan asignar referencias (Transforms) en el Inspector.");
            return;
        }

        int teamProCount = 0;
        int teamNoobCount = 0;

        foreach (var cursor in _cursors)
        {
            if (cursor.CurrentTeam == 1) teamProCount++;
            if (cursor.CurrentTeam == 2) teamNoobCount++;
        }

        if (teamProCount == 1 && teamNoobCount == 1)
        {
            StartGame();
        }
        else
        {
            Debug.Log("Aún no están listos para empezar (Falta gente o equipos incorrectos).");
        }
    }

    private void StartGame()
    {
        if (matchData == null)
        {
            Debug.LogError("Falta asignar el ScriptableObject 'MatchData' en el Inspector.");
            return;
        }
        if (gameConfig != null)
        {
            gameConfig.SetLocalMode();
        }
        else
        {
            Debug.LogError("¡Falta asignar GameConfigurationSO en TeamSelectionManager!");
        }

        foreach (var cursor in _cursors)
        {
            if (cursor.CurrentTeam != 0)
            {
                matchData.AddPlayerToTeam(cursor.Device, cursor.CurrentTeam);
            }
        }

        SceneManager.LoadScene(gameSceneName);
        Debug.Log("Iniciando Modo Local");

    }
}