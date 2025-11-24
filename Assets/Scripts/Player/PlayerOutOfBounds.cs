using UnityEngine;

public class PlayerOutOfBounds : MonoBehaviour
{
    [SerializeField] private string playerId = "P1";          // "P1" o "P2"
    [SerializeField] private PlayerManager playerManager;     // referencia por inspector

    private void OnBecameInvisible()
    {
        Debug.Log($"[Bounds] {playerId} salió de la cámara");

        if (playerManager != null)
        {
            playerManager.OnPlayerOutOfBounds(playerId);
        }
        else
        {
            Debug.LogWarning("[Bounds] No hay PlayerManager asignado en PlayerOutOfBounds");
        }
    }
}