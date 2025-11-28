using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class DashController : NetworkBehaviour
{
    [SerializeField] private float dashCooldown = 1.0f;
    [SerializeField] private float dashPushForce = 10f;
    [SerializeField] private float pushDuration = 0.2f;

    private MovementController _movement;
    private PlayerInputHandler _input;
    private bool _canDash = true;

    private void Awake()
    {
        _movement = GetComponent<MovementController>();
        _input = GetComponent<PlayerInputHandler>();
    }

    private void OnEnable() => _input.OnDashPressed += HandleDashPressed;
    private void OnDisable() => _input.OnDashPressed -= HandleDashPressed;

    private void HandleDashPressed(Vector2 direction)
    {
        if (_canDash && (!IsSpawned || IsOwner))
        {
            _movement.PerformDash(direction);
            StartCoroutine(DashCooldownCoroutine());
        }
    }

    private IEnumerator DashCooldownCoroutine()
    {
        _canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Filtros de seguridad
        if (IsSpawned && !IsOwner) return; // Si es online y no soy yo, ignoro
        if (!_movement.IsDashing) return;  // Solo empujo si estoy haciendo dash
        if (!collision.gameObject.CompareTag("Player")) return;

        MovementController enemy = collision.gameObject.GetComponent<MovementController>();
        if (enemy != null && !enemy.IsBeingPushed)
        {
            Vector2 pushDir = (collision.transform.position - transform.position).normalized;

            if (IsSpawned)
            {
                NetworkObject enemyNet = collision.gameObject.GetComponent<NetworkObject>();
                if (enemyNet != null)
                {
                    RequestPushEnemyServerRpc(enemyNet.NetworkObjectId, pushDir);
                }
            }
            else
            {
                enemy.GetPushed(pushDir, dashPushForce, pushDuration);
            }
        }
    }

    [ServerRpc]
    private void RequestPushEnemyServerRpc(ulong enemyId, Vector2 direction)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(enemyId, out NetworkObject enemyObj))
        {
            var enemyMove = enemyObj.GetComponent<MovementController>();
            if (enemyMove != null)
            {
                enemyMove.ApplyPushClientRpc(direction, dashPushForce, pushDuration);
            }
        }
    }
}