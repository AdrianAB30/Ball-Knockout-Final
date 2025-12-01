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
    private PlayerSquashStretch _visuals;

    private bool _canDash = true;
    private bool _isDead = true;
    public bool IsDead => _isDead;
    public bool IsDashing { get; private set; } = false;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
        _movement = GetComponent<MovementController>();
        _visuals = GetComponent<PlayerSquashStretch>();
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
        if (_isDead) return;
        if (!_canDash) return;

        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsOwner) return;

        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer)
        {
            RequestDashServerRpc(direction); 

            if (_visuals != null) _visuals.TriggerSquashAndStretch(direction);
        }
        else
        {
            ApplyDashForce(direction); 
            if (_visuals != null) _visuals.TriggerSquashAndStretch(direction);
        }

        StartCoroutine(DashCooldownCoroutine());
    }

    [ServerRpc]
    private void RequestDashServerRpc(Vector2 direction)
    {
        ApplyDashForce(direction);

        PlayDashVisualsClientRpc(direction);
    }

    [ClientRpc]
    private void PlayDashVisualsClientRpc(Vector2 direction)
    {
        if (IsOwner) return;

        if (_isDead) return;

        if (_visuals != null)
        {
            _visuals.TriggerSquashAndStretch(direction);
        }
    }

    private void ApplyDashForce(Vector2 dir)
    {
        if (_isDead) return;
        if (!_canDash) return;

        if (_movement != null) _movement.NotifyDashStarted();
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(dir * dashForce, ForceMode2D.Impulse);

        Debug.Log("¡DASH EJECUTADO!");
    }

    private IEnumerator DashCooldownCoroutine()
    {
        _canDash = false;
        IsDashing = true; 

        yield return new WaitForSeconds(0.3f);
        IsDashing = false; 

        yield return new WaitForSeconds(dashCooldown - 0.3f);
        _canDash = true;
    }
    public void SetDead(bool state)
    {
        _isDead = state;
    }
}