using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class LocalGameLauncher : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private string gameSceneName = "GameScene";

    public void StartLocalMatch()
    {
        gameConfig.SetLocalMode();

        Debug.Log("Iniciando partida LOCAL (Pantalla Dividida)...");

        StartCoroutine(ChangeSceneLocal());
    }
    private IEnumerator ChangeSceneLocal()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(gameSceneName);

    }
}