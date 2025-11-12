using UnityEngine;
using System;

public class KillZone : MonoBehaviour
{
    public static event Action<GameObject> OnPlayerKO;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"{other.name} ha sido eliminado!");

            OnPlayerKO?.Invoke(other.gameObject);

            Destroy(other.gameObject);
        }
    }
}