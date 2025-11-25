using UnityEngine;

public enum GameModeType
{
    OnlineMultiplayer, 
    LocalSplitScreen   
}
[CreateAssetMenu(fileName = "GameConfigurationSO", menuName = "ScriptableObjects/GameConfigurationSO", order = 4)]
public class GameConfigurationSO : ScriptableObject
{
    [Header("Configuración de la Sesión Actual")]
    public GameModeType CurrentGameMode;

    // public string SelectedMapName;
    // public int TimeLimit;
    // public bool IsTeamMode;

    public void SetLocalMode()
    {
        CurrentGameMode = GameModeType.LocalSplitScreen;
    }

    public void SetOnlineMode()
    {
        CurrentGameMode = GameModeType.OnlineMultiplayer;
    }
}
