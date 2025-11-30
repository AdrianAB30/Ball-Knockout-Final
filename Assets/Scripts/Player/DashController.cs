using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class DashController : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private float dashForce = 30f;
    [SerializeField] private float dashCooldown = 1.0f;

    private Rigidbody2D _rb;
    private PlayerInputHandler _input;
    private MovementController _movement;

    private bool _canDash = true;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
        _movement = GetComponent<MovementController>();
    }

    private void OnEnable()
    {
        if (_input != null)
        {
            _input.OnDashPressed += PerformDash;
        }
    }

    private void OnDisable()
    {
        if (_input != null)
        {
            _input.OnDashPressed -= PerformDash;
        }
    }

    private void PerformDash(Vector2 direction)
    {
        if (!_canDash) return;
        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsOwner) return;

        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer)
        {
            RequestDashServerRpc(direction);
        }
        else
        {
            ApplyDashForce(direction);
        }

        StartCoroutine(DashCooldownCoroutine());
    }

    [ServerRpc]
    private void RequestDashServerRpc(Vector2 direction)
    {
        ApplyDashForce(direction);
    }

    private void ApplyDashForce(Vector2 dir)
    {
        if (_movement != null) _movement.NotifyDashStarted();

        _rb.AddForce(dir * dashForce, ForceMode2D.Impulse);

        Debug.Log("🔥 ¡DASH EJECUTADO!");
    }

    private IEnumerator DashCooldownCoroutine()
    {
        _canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }
}