using UnityEngine;
using Unity.Netcode;

public class CollisionDamage : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private float minDamageVelocity = 15f;

    private float _lastHitTime;
    private float _hitCooldown = 0.5f;
    private DashController _myDash;

    private FeelManager _feelManager;

    private void Awake()
    {
        _myDash = GetComponent<DashController>();
        if (_myDash == null)
        {
            Debug.LogError("CollisionDamage: ¡Falta el componente DashController!");
        }
    }

    private void Start()
    {
        _feelManager = FindFirstObjectByType<FeelManager>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsServer) return;
        if (Time.time < _lastHitTime + _hitCooldown) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            var enemyDash = collision.gameObject.GetComponent<DashController>();
            var enemyHealth = collision.gameObject.GetComponent<Health>();
            if (enemyHealth == null) return;

            if (enemyDash != null && enemyDash.IsDashing && !_myDash.IsDashing) return;

            if (_myDash.IsDashing)
            {
                GetComponent<PlayerInputHandler>().TriggerImpactVibration();
                ApplyDamage(enemyHealth, 999f);
                return;
            }

            float impactVelocity = collision.relativeVelocity.magnitude;
            if (impactVelocity > minDamageVelocity)
            {
                Rigidbody2D myRb = GetComponent<Rigidbody2D>();
                Rigidbody2D otherRb = collision.gameObject.GetComponent<Rigidbody2D>();

                if (myRb.linearVelocity.magnitude > otherRb.linearVelocity.magnitude)
                {
                    GetComponent<PlayerInputHandler>().TriggerImpactVibration();

                    ApplyDamage(enemyHealth, impactVelocity);
                }
            }
        }
    }

    [ClientRpc]
    private void TriggerShakeClientRpc()
    {
        if (_feelManager != null) _feelManager.StartShake();
    }

    private void ApplyDamage(Health enemy, float force)
    {
        _lastHitTime = Time.time;
        Debug.Log($"GOLPE EXITOSO! Fuerza: {force}");

        var enemyInput = enemy.GetComponent<PlayerInputHandler>();
        if (enemyInput != null)
        {
            enemyInput.TriggerImpactVibration();
        }
        TriggerScreenShake();
        enemy.TakeDamage();
    }
    private void TriggerScreenShake()
    {
        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer)
        {
            TriggerShakeClientRpc();
        }
        else
        {
            if (_feelManager != null) _feelManager.StartShake();
        }
    }
}