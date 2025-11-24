using UnityEngine;
using MoreMountains.Feedbacks;

public class CameraShakeOnHit : MonoBehaviour
{
    public MMF_Player hitShakeFeedback;

    private void Reset()
    {
        hitShakeFeedback = GetComponent<MMF_Player>();
    }

    public void PlayShake()
    {
        if (hitShakeFeedback != null)
        {
            hitShakeFeedback.PlayFeedbacks();
        }
        else
        {
            Debug.LogWarning("CameraShakeOnHit: MMF_Player no asignado");
        }
    }
}