using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class TransitionTeamSelection : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private string gameSceneName = "TeamSelectionScene";

    public void StartLocalMatch()
    {
        gameConfig.SetLocalMode();

        Debug.Log("Iniciando partida LOCAL (Pantalla Dividida).");

        StartCoroutine(ChangeSceneSelectionTeam());
    }
    private IEnumerator ChangeSceneSelectionTeam()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(gameSceneName);

    }
}