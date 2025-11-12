using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton.SceneManager == null) return;

        NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
    }
    private void Start()
    {
        
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
                Debug.Log("Termino de cargar" + sceneEvent.SceneName + " , " + sceneEvent.ClientId);
                break;


            case SceneEventType.LoadEventCompleted:
                Debug.Log("Todos Terminaron de cargar la escenea" + sceneEvent.SceneName);
                break;

            
            default:
                break;
        }
    }

    public void LoadGameScene(string sceneName)
    {
        if(!NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Solo el host puede cambiar de escena");
            return;
        }
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
