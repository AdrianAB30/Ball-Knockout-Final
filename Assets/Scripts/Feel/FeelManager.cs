using MoreMountains.Feedbacks;
using UnityEngine;

public class FeelManager : MonoBehaviour
{
    [SerializeField] private MMF_Player shakeFeel;

    public void StartShake()
    {
        if (shakeFeel != null)
        {
            shakeFeel.PlayFeedbacks();
        }
        else
        {
            Debug.LogWarning("FeelManager: No se encontró ningún MMF_Player en la escena para hacer el shake.");
        }
    }
}