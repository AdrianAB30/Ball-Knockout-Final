using UnityEngine;
using Unity.Netcode;

public class CollisionDamage : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private float minDamageVelocity = 15f;

    private float _lastHitTime;
    private float _hitCooldown = 0.5f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsServer) return;

        if (Time.time < _lastHitTime + _hitCooldown) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            float impactVelocity = collision.relativeVelocity.magnitude;

            if (impactVelocity > minDamageVelocity)
            {
                Rigidbody2D myRb = GetComponent<Rigidbody2D>();
                Rigidbody2D otherRb = collision.gameObject.GetComponent<Rigidbody2D>();

                if (myRb.linearVelocity.magnitude > otherRb.linearVelocity.magnitude)
                {
                    _lastHitTime = Time.time;

                    var enemyHealth = collision.gameObject.GetComponent<Health>();
                    if (enemyHealth != null)
                    {
                        Debug.Log($"GOLPE! Velocidad: {impactVelocity}");
                        enemyHealth.TakeDamage();
                    }
                }
            }
        }
    }
}